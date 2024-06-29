using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class FlushAllCacheEventHandler(IStorageManager storage) : IEventHandler<FlushAllStoresEvent>
{
    public Task Handle(FlushAllStoresEvent @event, CancellationToken cancellationToken)
    {
        storage.ClearAll();
        return Task.CompletedTask;
    }
}