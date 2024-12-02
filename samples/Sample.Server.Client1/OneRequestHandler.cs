using Sample.Server.Contracts;
using Shiny.Mediator;

namespace Sample.Server.Client1;


[SingletonHandler]
public class OneRequestHandler : IRequestHandler<OneRequest, DateTimeOffset>
{
    public Task<DateTimeOffset> Handle(OneRequest request, CancellationToken cancellationToken)
        => Task.FromResult(DateTimeOffset.UtcNow); 
}