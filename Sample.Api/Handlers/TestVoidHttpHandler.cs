namespace Sample.Api.Handlers;

public record TestVoidRequest(int Number, string StringArg) : IRequest;


[RegisterHandler]
[MediatorHttpPost("/testvoid")]
public class TestVoidHttpHandler : IRequestHandler<TestVoidRequest>
{
    public Task Handle(TestVoidRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}