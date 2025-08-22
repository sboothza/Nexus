using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nexus.Library.Components;

public class RabbitMqSubscriber : Subscriber
{
    private IConnection? _connection;
    private IChannel? _channel;
    private Counter<int>? _receiveCount;
    private string? _consumerTag;
    
    public override string Type => "RabbitMqSubscriber";
    public string? QueueName { get; set; }
    public string? Url { get; set; }
    

    [JsonConstructor]
    public RabbitMqSubscriber()
    {
    }

    public RabbitMqSubscriber(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        base.Configure(manager);
        
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(QueueName) || string.IsNullOrEmpty(Url))
            throw new Exception("Name, QueueName and Url must be set");
        
        var factory = new ConnectionFactory
        {
            Uri = new Uri(Url!)
        };

        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            using (var activity = _activitySource?.StartActivity())
            {
                _receiveCount?.Add(1);
                _logger?.LogInformation("{Name} received message for {RoutingKey}", Name, ea.RoutingKey);
                activity?.SetTag("greeting", "Hello World!");
            }
            var body = ea.Body.ToArray();
            var bodyString = Encoding.UTF8.GetString(body);
            var message = new DataMessage
            {
                ExtraInfo = ea.RoutingKey,
                Data = bodyString
            };
            await _manager!.Query(BindingName!, message);
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };
        _consumerTag = _channel.BasicConsumeAsync(QueueName, false, consumer).Result;
    }

    public override Task<DataMessage?> Query(DataMessage input)
    {
        return Task.FromResult<DataMessage?>(null);
    }

    public override Task Ping()
    {
        return Task.CompletedTask;
    }

    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _receiveCount = meter.CreateCounter<int>($"RabbitMqSubscriber.{Name}.receive_count");
    }

    public override void Dispose()
    {
        if (_consumerTag != null)
            _channel?.BasicCancelAsync(_consumerTag).Wait();
        base.Dispose();
    }
}