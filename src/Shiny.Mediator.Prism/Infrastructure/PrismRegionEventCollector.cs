using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Prism.Infrastructure;


public class PrismRegionEventCollector(
    IRegionManager regionManager
) : IEventCollector
{
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        return regionManager
            .Regions.SelectMany(x => x.ActiveViews)
            .OfType<BindableObject>()
            .Select(x => x.BindingContext)
            .OfType<IEventHandler<TEvent>>()
            .ToList();
    }
}
