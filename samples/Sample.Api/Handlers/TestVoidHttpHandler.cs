namespace Sample.Api.Handlers;

public record TestVoidRequest(int Number, string StringArg) : IRequest;


[ScopedHandler]
[MediatorHttpPost("/testvoid", Name = "TestVoid")]
public class TestVoidHttpHandler : IRequestHandler<TestVoidRequest>
{
    public Task Handle(TestVoidRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}