using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;
using RabbitMQ.Client;

namespace Nexus.Library.Components;

public class RabbitMqPublisher : Publisher
{
    public override string Type => "RabbitMqPublisher";
    public string? Url { get; set; }
    public string? ExchangeName { get; set; }

    private IConnection? _connection;
    private IChannel? _channel;
    private Counter<int>? _publishCount;
    private Histogram<double>? _publishDuration;

    [JsonConstructor]
    public RabbitMqPublisher()
    {
    }

    public RabbitMqPublisher(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        base.Configure(manager);

        var factory = new ConnectionFactory
        {
            Uri = new Uri(Url!)
        };

        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;
    }

    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _publishCount = meter.CreateCounter<int>($"RabbitMqPublisher.{Name}.publish_count");
        _publishDuration = meter.CreateHistogram<double>($"RabbitMqPublisher.{Name}.publish_duration");
    }

    public async override Task<DataMessage?> Query(DataMessage message)
    {
        try
        {
            if (string.IsNullOrEmpty(message.Data) || string.IsNullOrEmpty(message.ExtraInfo?["topic"]))
                throw new Exception("Topic and Data must be set");
            
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };
            var stopwatch = Stopwatch.StartNew();
            var bytes = Encoding.UTF8.GetBytes(message.Data);
            await _channel!.BasicPublishAsync(ExchangeName!, message.ExtraInfo["topic"], mandatory: true,
                basicProperties: props, body: bytes);

            using (_activitySource?.StartActivity())
            {
                stopwatch.Stop();
                _publishCount?.Add(1);
                _publishDuration?.Record(stopwatch.Elapsed.TotalSeconds);
                _logger?.LogInformation("Call to {Name} completed in {ElapsedMilliseconds}ms", Name,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception e)
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
        if (_channel != null)
        {
            _channel.CloseAsync().Wait();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            _connection.CloseAsync().Wait();
            _connection.Dispose();
        }

        base.Dispose();
    }
}