namespace Nexus.Tests;

public class TestClient
{
    [Test]
    public void TestBasicClient()
    {
        var client = new Client.Client("http://localhost:50051");
        var result= client.Query<ApiRequest, ApiResponse>("BindingDoPost", new ApiRequest { Name = "Stephen" }).Result;
        Console.WriteLine(result!.Status);

    }
}