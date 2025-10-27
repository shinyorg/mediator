namespace Sample.Api.Handlers;

[MediatorScoped]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    [MediatorHttpGet("GetMyRequest", "/")]
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult("Test");
    }
}

public class MyRequest : IRequest<string>;