using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;

namespace Nexus.Library.Components;

public class KafkaSubscriber : Subscriber
{
    public string? Topic { get; set; }
    public string? GroupId { get; set; }
    public string[]? BootstrapServers { get; set; }
    public override string Type => "KafkaSubscriber";
    private IConsumer<Ignore, string>? _consumer;
    private Thread? _consumerThread;
    private readonly CancellationTokenSource _cancellationToken = new();
    private Counter<int>? _receiveCount;

    [JsonConstructor]
    public KafkaSubscriber()
    {
    }

    public KafkaSubscriber(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        base.Configure(manager);

        if (BootstrapServers is null || BootstrapServers.Length == 0)
            throw new Exception("BootstrapServers must be set");

        var servers = string.Join(',', BootstrapServers);
        var config = new ConsumerConfig
        {
            BootstrapServers = servers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe(Topic);
        _consumerThread = new Thread(ConsumeMessages);
        _consumerThread.Start();
    }

    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _receiveCount = meter.CreateCounter<int>($"KafkaSubscriber.{Name}.receive_count");
    }

    private void ConsumeMessages()
    {
        try
        {
            while (true)
            {
                try
                {
                    var cr = _consumer?.Consume(_cancellationToken.Token);
                    using (var activity = _activitySource?.StartActivity())
                    {
                        _receiveCount?.Add(1);
                        _logger?.LogInformation("{Name} received message for {Topic}", Name, cr?.Topic);
                        activity?.SetTag("greeting", "Hello World!");
                    }

                    var json = cr?.Message.Value!;
                    var message = new DataMessage
                    {
                        ExtraInfo = new Dictionary<string, string>() { { "topic", cr?.Topic! } },
                        Data = json
                    };
                    _manager!.Query(BindingName!, message).Wait();
                }
                catch (ConsumeException e)
                {
                    _logger!.LogError(e, "Error receiving message!");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _consumer?.Close(); // Ensure the consumer leaves the group cleanly
        }
    }

    public override Task<DataMessage?> Query(DataMessage input)
    {
        return Task.FromResult<DataMessage?>(null);
    }

    public override Task Ping()
    {
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        base.Dispose();
        _cancellationToken.Cancel();
        _consumerThread?.Join();
        _consumer?.Close();
    }
}