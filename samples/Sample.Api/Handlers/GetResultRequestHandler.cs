namespace Sample.Api.Handlers;


[MediatorHttpGroup("/thing")]
public class GetResultRequestHandler : IRequestHandler<GetThingRequest, string>, ICommandHandler<DoThing>, ICommandHandler<DoOtherThing>
{
    [MediatorHttpGet("GetThing", "/{parameter}")]
    public Task<string> Handle(GetThingRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult($"Route: {request.Parameter} - Query: {request.Query}");


    [MediatorHttpPut("DoThing", "/")]
    public Task Handle(DoThing command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    // this should not be mapped to aspnet
    public Task Handle(DoOtherThing command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class DoThing : ICommand;

public class DoOtherThing : ICommand;

public class GetThingRequest : IRequest<string>
{
    public string? Parameter { get; set; }
    public string? Query { get; set; }
}