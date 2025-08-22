using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;

namespace Nexus.Library.Components;

public class KafkaPublisher : Publisher
{
    public override string Type => "KafkaPublisher";

    public string[]? BootstrapServers { get; set; }

    private IProducer<Null, string>? _producer;
    private Counter<int>? _publishCount;
    private Histogram<double>? _publishDuration;

    [JsonConstructor]
    public KafkaPublisher()
    {
    }

    public KafkaPublisher(ILogger logger) : base(logger)
    {
    }
    
    public override void Configure(Manager manager)
    {
        base.Configure(manager);
        
        if (BootstrapServers is null || BootstrapServers.Length == 0)
            throw new Exception("BootstrapServers must be set");
        
        var servers = string.Join(',', BootstrapServers);
        var config = new ProducerConfig
        {
            BootstrapServers = servers
        };
        
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }
    
    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _publishCount = meter.CreateCounter<int>($"KafkaPublisher.{Name}.publish_count");
        _publishDuration = meter.CreateHistogram<double>($"KafkaPublisher.{Name}.publish_duration");
    }

    public async override Task<DataMessage?> Query(DataMessage message)
    {
        try
        {
            if (string.IsNullOrEmpty(message.Data) || string.IsNullOrEmpty(message.Metadata["topic"]))
                throw new Exception("Topic and Data must be set");
            
            var stopwatch = Stopwatch.StartNew();
            var deliveryResult = await _producer!.ProduceAsync(message.Metadata["topic"], new Message<Null, string>
            {
                Value = message.Data!,
            });
            using (var activity = _activitySource?.StartActivity())
            {
                stopwatch.Stop();
                _publishCount?.Add(1);
                _publishDuration?.Record(stopwatch.Elapsed.TotalSeconds);
                _logger?.LogInformation("{Name} Delivered '{Value}' to '{TopicPartitionOffset}' completed in {ElapsedMilliseconds}ms", Name, deliveryResult.Value, deliveryResult.TopicPartitionOffset, stopwatch.ElapsedMilliseconds);
                activity?.SetTag("greeting", "Hello World!");
            }
        }
        catch (ProduceException<Null, string> e)
        {
            _logger!.LogError(e, "Error publishing message!");
        }

        return null;
    }
    
    public override Task Ping()
    {
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        base.Dispose();
        _producer?.Dispose();
    }
}