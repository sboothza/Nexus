using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;
using StackExchange.Redis;

namespace Nexus.Library.Components;

public class RedisStateStore : StateStore
{
    public string? HostName { get; set; }
    public int? ExpirySeconds { get; set; }
    private ConnectionMultiplexer? _redis;
    private IDatabase? _db;
    private Counter<int>? _cacheGetCount;
    private Counter<int>? _cacheSetCount;
    private Histogram<double>? _callDuration;

    [JsonConstructor]
    public RedisStateStore()
    {
    }

    public RedisStateStore(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        if (string.IsNullOrEmpty(HostName) || ExpirySeconds is null)
            throw new InvalidOperationException("HostName and ExpirySeconds must be set");

        base.Configure(manager);
        _redis = ConnectionMultiplexer.Connect(HostName!);
        _db = _redis.GetDatabase();
    }

    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _cacheGetCount = meter.CreateCounter<int>($"RedisStateStore.{Name}.get_count");
        _cacheSetCount = meter.CreateCounter<int>($"RedisStateStore.{Name}.set_count");
        _callDuration = meter.CreateHistogram<double>($"RedisStateStore.{Name}.call_duration");
    }

    protected async override Task<string?> GetValueAsync(string key)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            var value = await _db!.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return null;

            _cacheGetCount?.Add(1);
            success = true;
            return value;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error getting value for key {Key}", key);
            success = false;
            throw;
        }
        finally
        {
            using (_activitySource?.StartActivity())
            {
                stopwatch.Stop();
                _callDuration?.Record(stopwatch.Elapsed.TotalSeconds);
                _logger?.LogInformation("State store {Name} accessed {success} completed in {ElapsedMilliseconds}ms",
                    Name,
                    success ? "successfully" : "unsuccessfully", stopwatch.ElapsedMilliseconds);
            }
        }
    }

    protected async override Task SetValueAsync(string key, string value)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            await _db?.StringSetAsync(key, value, TimeSpan.FromSeconds(ExpirySeconds!.Value))!;
            _cacheSetCount?.Add(1);
            success = true;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error setting value for key {Key}", key);
            success = false;
            throw;
        }
        finally
        {
            using (_activitySource?.StartActivity())
            {
                stopwatch.Stop();
                _callDuration?.Record(stopwatch.Elapsed.TotalSeconds);
                _logger?.LogInformation("State store {Name} set {success} completed in {ElapsedMilliseconds}ms",
                    Name,
                    success ? "successfully" : "unsuccessfully", stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _redis?.Close();
        _redis?.Dispose();
    }
}