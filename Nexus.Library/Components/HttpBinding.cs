using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;
using Polly;
using Polly.CircuitBreaker;

namespace Nexus.Library.Components;



public class HttpBinding : Binding
{
    public override string Type => "HttpBinding";

    public string? Url { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int? AllowedExceptions { get; set; }
    public int? BreakDurationSeconds { get; set; }

    public string? Method { get; set; }
    public string? JsonNamingPolicy { get; set; }

    private JsonNamingPolicy? _jsonNamingPolicy;

    public Authentication? Authentication { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    private AsyncPolicy? _circuitBreakerPolicy;

    private Counter<int>? _callCount;
    private Histogram<double>? _callDuration;
    private JsonSerializerOptions? _jsonOptions;
    private HttpClient? _client;
    private HttpMethod? _method;
    private Uri? _requestUri;

    [JsonConstructor]
    public HttpBinding()
    {
    }

    public HttpBinding(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(Method) || string.IsNullOrEmpty(JsonNamingPolicy))
            throw new Exception("Url, Method, JsonNamingPolicy must be set");

        base.Configure(manager);

        _jsonNamingPolicy = JsonNamingPolicy!.GetJsonNamingPolicy();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = _jsonNamingPolicy,
            PropertyNameCaseInsensitive = true
        };

        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds!.Value)
        };

        if (_client == null)
            throw new Exception("Client could not be created");

        _method = HttpMethod.Parse(Method);
        _requestUri = new Uri(Url!);

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

        if (_circuitBreakerPolicy == null)
            throw new Exception("Circuit breaker could not be created");

        if (Authentication == null)
            throw new Exception("Authentication must be set");
    }

    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _callCount = meter.CreateCounter<int>($"HttpBinding.{Name}.call_count");
        _callDuration = meter.CreateHistogram<double>($"HttpBinding.{Name}.call_duration");
    }

    public override string ToString()
    {
        return $"{Type} {Url} {TimeoutSeconds} {Method} {Headers!.ExpandToString()}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async override Task<DataMessage?> Query(DataMessage input)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        var message = new HttpRequestMessage
        {
            Method = _method!,
            RequestUri = _requestUri
        };

        message.Content = new StringContent(input.Data!, Encoding.UTF8, "application/json");

        Headers!.AddHeaders(message.Headers);
        
        if (Authentication!.AuthType != AuthType.None)
            message.Headers.Add("Authorization", Authentication.GetAuthHeader());

        try
        {
            var responseMessage =
                await _circuitBreakerPolicy!.ExecuteAsync(async () => await _client!.SendAsync(message));
            if (responseMessage.IsSuccessStatusCode)
            {
                success = true;
                var jsonResult = "";
                try
                {
                    jsonResult = await responseMessage.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Error occurred while executing request");
                }

                return new DataMessage
                {
                    Success = true,
                    Data = jsonResult,
                    ExtraInfo = responseMessage.Headers.ToDict()
                };
            }
            else
            {
                _logger?.LogError("Error occurred while executing request: {StatusCode}",
                    responseMessage.StatusCode);
                return new ErrorMessage
                {
                    Error = responseMessage.ReasonPhrase,
                    Success = false,
                    ExtraInfo = responseMessage.Headers.ToDict()
                };
            }
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

    public override void Dispose()
    {
        base.Dispose();
        _client!.Dispose();
    }
}