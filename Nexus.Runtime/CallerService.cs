using Grpc.Core;
using GrpcCaller;
using Microsoft.Extensions.Logging;
using Nexus.Library;
using Nexus.Library.Components;
using Nexus.Library.Modules;

namespace Nexus.Runtime;

public class CallerService(ILogger<CallerService> logger, Manager manager) : Caller.CallerBase
{
    private readonly ILogger<CallerService> _logger = logger;

    public override async Task<QueryResponse> Query(QueryRequest request, ServerCallContext context)
    {
        var result = await manager.Query(request.BindingName, request.ToDataMessage());
        if (result != null)
        {
            var response = new QueryResponse();
            result.ExtraInfo.ToMap(response.ExtraInfo);
            response.Data = result.Data;
        }

        return new QueryResponse();
    }
}