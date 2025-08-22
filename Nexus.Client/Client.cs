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

    public async Task<TR?> Query<T, TR>(string bindingName, T input, ExtraInfo? extraInfo = null)
    {
        var extra = "";
        if (extraInfo is not null)
            extra = JsonSerializer.Serialize(input, _options);

        var request = new QueryRequest
        {
            BindingName = bindingName,
            Data = JsonSerializer.Serialize(input),
            ExtraInfo = extra
        };
        var response = await _client.QueryAsync(request);
        if (string.IsNullOrEmpty(response.Data))
            return default;
        Console.WriteLine(typeof(TR).Name);
        var obj = JsonSerializer.Deserialize<TR>(response.Data, _options);
        return obj;
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}