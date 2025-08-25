using System.Text.Json;
using Grpc.Net.Client;
using GrpcCaller;

namespace Nexus.Client;

public class Client : IDisposable
{
    private readonly Caller.CallerClient _client;
    private readonly GrpcChannel _channel;
    private readonly JsonSerializerOptions _options;

    public Client(string serverAddress)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new Caller.CallerClient(_channel);
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<TR?> Query<T, TR>(string bindingName, T input, Dictionary<string, string>? extraInfo = null)
    {
        var request = new QueryRequest
        {
            BindingName = bindingName,
            Data = JsonSerializer.Serialize(input),
        };

        extraInfo.ToMap(request.ExtraInfo);
        var response = await _client.QueryAsync(request);
        if (!response.Success)
        {
            var ex = new RemoteException(response.ExtraInfo["error"]!, response.ExtraInfo["stackTrace"]!);
            throw ex;
        }

        if (string.IsNullOrEmpty(response.Data))
            return default;
        Console.WriteLine(typeof(TR).Name);
        var obj = JsonSerializer.Deserialize<TR>(response.Data, _options);
        return obj;
    }

    public async Task<string> GetValue(string storeName, string key)
    {
        var request = new QueryRequest
        {
            BindingName = storeName,
        };
        request.ExtraInfo.Add("key", key);
        request.ExtraInfo.Add("operation", "get");
        var response = await _client.QueryAsync(request);
        if (!response.Success)
        {
            var ex = new RemoteException(response.ExtraInfo["error"]!, response.ExtraInfo["stackTrace"]!);
            throw ex;
        }

        return response.Data;
    }

    public async Task SetValue(string storeName, string key, string value)
    {
        var request = new QueryRequest
        {
            BindingName = storeName,
        };
        request.ExtraInfo.Add("key", key);
        request.ExtraInfo.Add("operation", "set");
        request.Data = value;
        var response = await _client.QueryAsync(request);
        if (!response.Success)
        {
            var ex = new RemoteException(response.ExtraInfo["error"]!, response.ExtraInfo["stackTrace"]!);
            throw ex;
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}