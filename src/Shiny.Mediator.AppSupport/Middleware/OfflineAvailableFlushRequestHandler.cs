using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineAvailableFlushRequestHandler(IStorageService storage) : IRequestHandler<OfflineAvailableFlushRequest>
{
    public Task Handle(OfflineAvailableFlushRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask; //storage.Clear();
}