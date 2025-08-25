using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nexus.Library.Components;

public abstract class StateStore : Component
{
    [JsonConstructor]
    protected StateStore()
    {
    }

    protected StateStore(ILogger logger) : base(logger)
    {
    }

    public async override Task<DataMessage?> Query(DataMessage message)
    {
        if (string.IsNullOrEmpty(message.ExtraInfo?["key"]) || string.IsNullOrEmpty(message.ExtraInfo?["operation"]))
            throw new Exception("Key and Operation must be set");
        
        if (message.ExtraInfo["operation"] == "get")
        {
            var value = await GetValueAsync(message.ExtraInfo["key"]);
            return new DataMessage
            {
                Data = value,
                Success = true,
            };
        }

        if (message.ExtraInfo["operation"] == "set")
        {
            if (string.IsNullOrEmpty(message.Data))
                throw new Exception("Data must be set");
            
            await SetValueAsync(message.ExtraInfo["key"], message.Data!);
            return new DataMessage
            {
                Success = true,
            };
        }
        
        throw new Exception("Operation must be get or set");
    }
    
    public override Task Ping()
    {
        return Task.CompletedTask;
    }

    protected abstract Task<string?> GetValueAsync(string key);

    protected abstract Task SetValueAsync(string key, string value);
}