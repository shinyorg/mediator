using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Uno.Infrastructure;


public class UnoEventCollector(INavigator navigator) : IEventCollector
{
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        throw new NotImplementedException();
    }
}