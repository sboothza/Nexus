using Grpc.Core;
using GrpcCaller;
using Microsoft.Extensions.Logging;
using Nexus.Library;
using Nexus.Library.Components;
using Nexus.Library.Modules;

namespace Nexus.Runtime;

public class CallerService : Caller.CallerBase
{
    private readonly ILogger<CallerService> _logger;
    private readonly Manager _manager;

    public CallerService(ILogger<CallerService> logger, Manager manager)
    {
        _logger = logger;
        _manager = manager;
    }

    public async override Task<QueryResponse> Query(QueryRequest request, ServerCallContext context)
    {
        var result = await _manager.Query(request.BindingName, request.ToDataMessage());
        if (result != null)
            return new QueryResponse
            {
                ExtraInfo = result.ExtraInfo,
                Data = result.Data,
            };
        return new QueryResponse();
    }

    
}