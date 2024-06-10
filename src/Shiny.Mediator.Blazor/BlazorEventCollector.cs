using Microsoft.AspNetCore.Components;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor;

public class BlazorEventCollector(NavigationManager navigator) : IEventCollector, IComponentActivator
{
    IComponent? currentComponent;
    
    
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        if (this.currentComponent is IEventHandler<TEvent> handler)
            return [handler];
        
        return [];
    }

    
    public IComponent CreateInstance(Type componentType)
    {
        var component = (IComponent)Activator.CreateInstance(componentType);
        this.currentComponent = component;

        return component;
    }
}