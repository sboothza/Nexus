using Grpc.Core;
using GrpcCaller;
using Microsoft.Extensions.Logging;
using Nexus.Library.Modules;

namespace Nexus.Runtime;

public class CallerService(ILogger<CallerService> logger, Manager manager) : Caller.CallerBase
{
    public async override Task<QueryResponse> Query(QueryRequest request, ServerCallContext context)
    {
        logger.LogInformation("Query called for {bindingName}", request.BindingName);
        var result = await manager.Query(request.BindingName, request.ToDataMessage());
        if (result != null)
        {
            var response = result.ToQueryResponse();
            logger.LogInformation("Query completed for {bindingName}", request.BindingName);
            return response;
        }

        return new QueryResponse();
    }
}