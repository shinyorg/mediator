using Microsoft.AspNetCore.Components;

namespace Shiny.Mediator.Blazor;

public class BlazorEventCollector(NavigationManager navigator) : IEventCollector, Microsoft.AspNetCore.Components.IComponentActivator
{
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        navigator.LocationChanged += (sender, args) =>
        {

        };
        navigator.RegisterLocationChangingHandler(async (context) =>
        {
        });
        
        // TODO: no idea yet
        return null;
    }

    public IComponent CreateInstance(Type componentType)
    {
        throw new NotImplementedException();
    }
}