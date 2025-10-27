namespace Sample.Api.Handlers;


[MediatorScoped]
[MediatorHttpGroup("/thing")]
public class GetResultRequestHandler : IRequestHandler<GetThingRequest, GetThingResult>, ICommandHandler<DoThing>, ICommandHandler<DoOtherThing>
{
    [MediatorHttpGet("GetThing", "/{parameter}")]
    public Task<GetThingResult> Handle(GetThingRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetThingResult(request.Parameter, request.Query));
    }

    [MediatorHttpPut("DoThing", "/")]
    public Task Handle(DoThing command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    // this should not be mapped to aspnet
    public Task Handle(DoOtherThing command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class DoThing : ICommand;

public class DoOtherThing : ICommand;

public class GetThingRequest : IRequest<GetThingResult>
{
    public string? Parameter { get; set; }
    public string? Query { get; set; }
}

public record GetThingResult(string? Parameter, string? Query);