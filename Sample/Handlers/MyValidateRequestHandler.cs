using Sample.Contracts;

namespace Sample.Handlers;


[SingletonHandler]
public class MyValidateRequestHandler : IRequestHandler<MyValidateRequest>
{
    public Task Handle(MyValidateRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}