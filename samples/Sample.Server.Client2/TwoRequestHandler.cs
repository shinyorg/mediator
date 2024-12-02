using Sample.Server.Contracts;
using Shiny.Mediator;

namespace Sample.Server.Client2;


[SingletonHandler]
public class TwoRequestHandler : IRequestHandler<TwoRequest, DateTimeOffset>
{
    public Task<DateTimeOffset> Handle(TwoRequest request, CancellationToken cancellationToken)
        => Task.FromResult(DateTimeOffset.UtcNow); 
}