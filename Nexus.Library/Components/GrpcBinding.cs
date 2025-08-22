using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;
using Polly;
using Polly.CircuitBreaker;

namespace Nexus.Library.Components;

public class GrpcBinding : Binding
{
    public override string Type => "GrpcBinding";
    public string? Address { get; set; }
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int? AllowedExceptions { get; set; }
    public int? BreakDurationSeconds { get; set; }

    private AsyncPolicy? _circuitBreakerPolicy;
    private Counter<int>? _callCount;
    private Histogram<double>? _callDuration;
    private DynamicGrpcClient? _client;

    [JsonConstructor]
    public GrpcBinding()
    {
    }

    public GrpcBinding(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        if (string.IsNullOrEmpty(Address) || string.IsNullOrEmpty(ServiceName) || string.IsNullOrEmpty(MethodName))
            throw new Exception(
                "Address, ServiceName, MethodName, CircuitBreakerPolicy and JsonNamingPolicy must be set");

        base.Configure(manager);

        _circuitBreakerPolicy = Policy.Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: AllowedExceptions!.Value,
                durationOfBreak: TimeSpan.FromSeconds(BreakDurationSeconds!.Value),
                onBreak: (ex, breakDelay) =>
                {
                    _logger?.LogWarning(
                        "Circuit breaker opened: {Arg1Message}, duration: {BreakDelayTotalSeconds} seconds",
                        ex.Message,
                        breakDelay.TotalSeconds);
                },
                onReset: () => { _logger?.LogWarning("Circuit breaker reset."); },
                onHalfOpen: () => { _logger?.LogWarning("Circuit breaker half-open."); }
            );

        _client = new DynamicGrpcClient();
    }

    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _callCount = meter.CreateCounter<int>($"GrpcBinding.{Name}.call_count");
        _callDuration = meter.CreateHistogram<double>($"GrpcBinding.{Name}.call_duration");
    }

    public override string ToString()
    {
        return $"{Type} {Address} {TimeoutSeconds}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async override Task<DataMessage?> Query(DataMessage input) 
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            var result = await _circuitBreakerPolicy!.ExecuteAsync(async () =>
                await _client!.InvokeAsync<DataMessage, DataMessage>(Address!, ServiceName!, MethodName!, input));
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger?.LogError(ex, "Broken circuit while invoking {Name}", Name);
            throw;
        }
        catch (TaskCanceledException tce)
        {
            _logger?.LogError(tce, "Timeout while invoking {Name}", Name);
            throw;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error while invoking {Name}", Name);
            throw;
        }
        finally
        {
            using (_activitySource?.StartActivity())
            {
                stopwatch.Stop();
                _callCount?.Add(1);
                _callDuration?.Record(stopwatch.Elapsed.TotalSeconds);
                _logger?.LogInformation("Call to {Name} {success} completed in {ElapsedMilliseconds}ms", Name,
                    success ? "successfully" : "unsuccessfully", stopwatch.ElapsedMilliseconds);
            }
        }
    }
    
    public override Task Ping()
    {
        return Task.CompletedTask;
    }
}