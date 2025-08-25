using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;

namespace Nexus.Library.Components;

/// <summary>
/// State store implemented in MSSQL
/// NOTE: Requires DDL access as well as rw
/// </summary>
public class SqlStateStore : StateStore
{
    public override string Type => "SqlStateStore";
    public string? ConnectionString { get; set; }
    public string? TableName { get; set; }
    public int ExpirySeconds { get; set; }
    private Counter<int>? _cacheHitCount;
    private Counter<int>? _cacheMissCount;
    private Counter<int>? _cacheSetCount;
    private Histogram<double>? _callDuration;
    private SqlConnection? _connection;

    [JsonConstructor]
    public SqlStateStore()
    {
    }

    public SqlStateStore(ILogger logger) : base(logger)
    {
    }

    public override void Configure(Manager manager)
    {
        if (string.IsNullOrEmpty(ConnectionString) || string.IsNullOrEmpty(TableName))
            throw new Exception("ConnectionString and TableName must be set");

        base.Configure(manager);

        _connection = new SqlConnection(ConnectionString);
        _connection.Open();
        CreateTable();
    }

    private void CreateTable()
    {
        try
        {
            var cmd = _connection?.CreateCommand();
            cmd!.CommandText =
                $"IF NOT EXISTS (SELECT * FROM SYSOBJECTS WHERE NAME = '{TableName}' AND XTYPE = 'U') CREATE TABLE [{TableName}] ([Key] VARCHAR(255) PRIMARY KEY, [Value] NVARCHAR(MAX), [Expiry] DATETIME);";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException e)
        {
            _logger?.LogError(e, "Error creating table {TableName}", TableName);
            throw;
        }
    }

    public override void CreateMetrics(Meter meter)
    {
        base.CreateMetrics(meter);
        _cacheHitCount = meter.CreateCounter<int>($"SqlStateStore.{Name}.hit_count");
        _cacheMissCount = meter.CreateCounter<int>($"SqlStateStore.{Name}.miss_count");
        _cacheSetCount = meter.CreateCounter<int>($"SqlStateStore.{Name}.set_count");
        _callDuration = meter.CreateHistogram<double>($"SqlStateStore.{Name}.call_duration");
    }

    protected async override Task<string?> GetValueAsync(string key) 
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            var cmd = _connection?.CreateCommand();
            cmd!.CommandText = $"SELECT [Value], [Expiry] FROM [{TableName}] WHERE [Key] = @Key";
            cmd.Parameters.AddWithValue("@Key", key);
            var result = await cmd.ExecuteReaderAsync();
            if (result is null)
                return null;

            if (await result.ReadAsync())
            {
                var value = (string)result["Value"];
                var expiry = result["Expiry"];
                await result.CloseAsync();
                success = true;
                if (expiry is DBNull || expiry is DateTime dt && dt < DateTime.Now)
                {
                    cmd.CommandText = $"DELETE FROM [{TableName}] WHERE [Key] = @Key";
                    await cmd.ExecuteNonQueryAsync();
                    _cacheHitCount?.Add(1);
                    return null;
                }

                cmd.CommandText = $"UPDATE [{TableName}] SET [Expiry] = @Expiry WHERE [Key] = @Key";
                cmd.Parameters.AddWithValue("@Expiry", DateTime.Now.AddSeconds(ExpirySeconds));
                await cmd.ExecuteNonQueryAsync();
                _cacheMissCount?.Add(1);
                return value;
            }

            return null;
        }
        catch (SqlException sex)
        {
            _logger?.LogError(sex, "Error getting value for key {Key}", key);
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
            var cmd = _connection?.CreateCommand();
            cmd!.CommandText = $"DELETE FROM [{TableName}] WHERE [Key] = @Key";
            cmd.Parameters.AddWithValue("@Key", key);
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = $"INSERT INTO [{TableName}] ([Key], [Value], [Expiry]) VALUES (@Key, @Value, @Expiry)";
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.Parameters.AddWithValue("@Expiry", DateTime.Now.AddSeconds(ExpirySeconds));
            await cmd.ExecuteNonQueryAsync();
            _cacheSetCount?.Add(1);
            success = true;
        }
        catch (SqlException sex)
        {
            _logger?.LogError(sex, "Error setting value for key {Key}", key);
            success = false;
            throw;
        }
        finally
        {
            using (var activity = _activitySource?.StartActivity())
            {
                stopwatch.Stop();
                _callDuration?.Record(stopwatch.Elapsed.TotalSeconds);
                _logger?.LogInformation("State store {Name} set {success} completed in {ElapsedMilliseconds}ms",
                    Name,
                    success ? "successfully" : "unsuccessfully", stopwatch.ElapsedMilliseconds);
                activity?.SetTag("greeting", "Hello World!");
            }
        }
    }
}