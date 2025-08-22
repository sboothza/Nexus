using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NCrontab;
using Nexus.Library.Modules;

namespace Nexus.Library.Components;

public class Schedule : Component
{
    public override string Type => "Schedule";

    public string? BindingName { get; set; }
    public string? OnStartTrigger { get; set; }

    [JsonIgnore]
    private TimeSpan? _startDelayTimeSpan =>
        TimeSpan.TryParseExact(OnStartTrigger, "h\\hmm", CultureInfo.InvariantCulture, out var result)
            ? result
            : null;

    public string? Cron { get; set; }

    private CrontabSchedule? _schedule;

    [JsonConstructor]
    public Schedule()
    {
    }

    public Schedule(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        base.Configure(manager);
        
        _schedule = CrontabSchedule.Parse(Cron);
        if (_startDelayTimeSpan != null)
            NextTrigger = DateTime.Now + _startDelayTimeSpan.Value;
        else
            NextTrigger = _schedule.GetNextOccurrence(DateTime.Now);
    }

    protected virtual void CalculateNextTrigger()
    {
        NextTrigger = _schedule!.GetNextOccurrence(DateTime.Now);
    }
    
    public override Task<DataMessage?> Query(DataMessage input)
    {
        return Task.FromResult<DataMessage?>(null);
    }

    public async override Task Ping()
    {
        if (NextTrigger <= DateTime.Now)
        {
            _logger.LogInformation("Invoking schedule {ItemName}", Name);
            try
            {
                _manager.Query(BindingName!, new DataMessage()).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking schedule {ItemName}", Name);
            }

            CalculateNextTrigger();
        }
    }

    [JsonIgnore]
    public DateTime NextTrigger { get; private set; }

    public override string ToString()
    {
        return $"{Type} {Cron} {OnStartTrigger}";
    }
}