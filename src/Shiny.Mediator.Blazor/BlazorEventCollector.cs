using Microsoft.AspNetCore.Components;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor;


public class BlazorEventCollector : IEventCollector, IComponentActivator
{
    readonly List<WeakReference<IComponent>> components = new();
    
    
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        var returns = new List<IEventHandler<TEvent>>();
        lock (this.components)
        {
            var toRemove = new List<WeakReference<IComponent>>();
            foreach (var component in this.components)
            {
                if (component.TryGetTarget(out var c))
                {
                    if (c is IEventHandler<TEvent> e)
                    {
                        returns.Add(e);
                    }
                }
                else
                {
                    toRemove.Add(component);
                }
            }

            foreach (var rem in toRemove)
                this.components.Remove(rem);
        }
        return returns;
    }

    
    public IComponent CreateInstance(Type componentType)
    {
        var component = (IComponent)Activator.CreateInstance(componentType);
        lock (this.components)
            this.components.Add(new WeakReference<IComponent>(component));
        
        return component;
    }
}