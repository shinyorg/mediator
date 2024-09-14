namespace Sample.Api.Handlers;

[ScopedHandler]
[MediatorHttpGet("/getthing/{parameter}", Name = "GetThing")]
public class GetResultRequestHandler : IRequestHandler<GetThingRequest, string>
{
    public Task<string> Handle(GetThingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Route: {request.Parameter} - Query: {request.Query}");
    }
}

public class GetThingRequest : IRequest<string>
{
    public string? Parameter { get; set; }
    public string? Query { get; set; }
}