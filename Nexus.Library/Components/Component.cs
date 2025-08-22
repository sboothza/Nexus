using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;

namespace Nexus.Library.Components;

public abstract class Component : IDisposable
{
    protected ILogger? _logger;
    protected Manager? _manager;
    public string? Name { get; set; }
    public virtual string? Type { get; set; }
    protected ActivitySource? _activitySource;

    public virtual void Configure(Manager manager)
    {
        _manager = manager;
    }

    public void SetLogger(ILogger logger) => _logger = logger;

    public virtual void CreateMetrics(Meter meter)
    {
        _activitySource = new ActivitySource($"{meter.Name}-{Name}", "1.0.0");
    }

    [JsonConstructor]
    public Component()
    {
    }

    protected Component(ILogger logger)
    {
        _logger = logger;
    }
    
    public abstract Task<DataMessage?> Query(DataMessage input);

    public abstract Task Ping();

    public virtual void Dispose()
    {
        _activitySource?.Dispose();
    }
}