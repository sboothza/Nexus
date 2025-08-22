using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Nexus.Library.Components;

namespace Nexus.Library.Modules;

public class Manager : IDisposable
{
    private readonly Dictionary<string, Component> _components;
    private readonly Thread _schedulerThread;
    private bool _terminate;
    private readonly ILogger _logger;

    public Manager(string folder, ILogger logger, Meter meter)
    {
        _logger = logger;
        var components = ConfigParser.ParseList(folder, _logger, meter, this);
        _components = components.ToDictionary(a => a!.Name!, a => a!);

        _schedulerThread = new Thread(Run);
        _logger.LogInformation("Starting scheduler thread");
        _schedulerThread.Start();
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping scheduler thread");
        _terminate = true;
    }

    private void Run()
    {
        var schedules = _components.Values
            .OfType<Schedule>()
            .Select(a => a)
            .Distinct()
            .ToList();

        while (!_terminate)
        {
            foreach (var item in schedules)
                try
                {
                    item.Ping();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error ping schedule {Name}",  item!.Name);
                    throw;
                }

            Thread.Sleep(1000); //could be longer, but then thread is unresponsive
        }

        _schedulerThread.Join(1500);
    }

    public async Task<DataMessage?> Query(string name, DataMessage input)
    {
        _logger.LogInformation("Invoking {Name} with input {Input}", name, input);
        if (!_components.TryGetValue(name, out var binding))
        {
            _logger.LogError("Component {Name} not found", name);
            throw new Exception($"Component {name} not found");
        }

        var output = await binding.Query(input);
        return output;
    }

    private void ReleaseUnmanagedResources()
    {
        Stop();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Manager()
    {
        ReleaseUnmanagedResources();
    }
}