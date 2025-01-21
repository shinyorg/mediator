namespace Sample.Api.Handlers;

[ScopedHandler]
[MediatorHttpGet("GetThing", "/getthing/{parameter}")]
public class GetResultRequestHandler : IRequestHandler<GetThingRequest, string>
{
    public Task<string> Handle(GetThingRequest request, RequestContext<GetThingRequest> context, CancellationToken cancellationToken)
        => Task.FromResult($"Route: {request.Parameter} - Query: {request.Query}");
}

public class GetThingRequest : IRequest<string>
{
    public string? Parameter { get; set; }
    public string? Query { get; set; }
}