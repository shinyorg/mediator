using Microsoft.AspNetCore.Components;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


/// <summary>
/// Blazor <see cref="IEventCollector"/> that also acts as the framework's
/// <see cref="IComponentActivator"/>: every activated <see cref="IComponent"/> implementing
/// <see cref="IEventHandler{TEvent}"/> is tracked via a <see cref="WeakReference{T}"/> so events
/// can be dispatched to live components without leaking disposed ones.
/// </summary>
public class BlazorEventCollector : IEventCollector, IComponentActivator
{
    readonly List<WeakReference<IComponent>> components = new();


    /// <inheritdoc/>
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

    
    /// <inheritdoc/>
    public IComponent CreateInstance(Type componentType)
    {
        var component = (IComponent)Activator.CreateInstance(componentType)!;
        var isHandler = componentType
            .GetInterfaces()
            .Any(x => 
                x.IsGenericType && 
                x.GetGenericTypeDefinition() == typeof(IEventHandler<>)
            );
        
        if (isHandler)
        {
            lock (this.components)
                this.components.Add(new WeakReference<IComponent>(component));
        }

        return component;
    }
}