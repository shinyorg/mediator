using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Prism.Infrastructure;


/// <summary>
/// <see cref="IEventCollector"/> that discovers event handlers by walking the active views of
/// every Prism region and returning their binding contexts that implement
/// <see cref="IEventHandler{TEvent}"/>.
/// </summary>
public class PrismRegionEventCollector(
    IRegionManager regionManager
) : IEventCollector
{
    /// <inheritdoc/>
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
