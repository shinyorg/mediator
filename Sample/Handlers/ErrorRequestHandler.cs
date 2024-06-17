using Sample.Contracts;

namespace Sample.Handlers;


[RegisterHandler]
public class ErrorRequestHandler : IRequestHandler<ErrorRequest>
{
    public Task Handle(ErrorRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Why you call me?");
    }
}