using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Contracts;

namespace Shiny.Mediator.Impl;

// singleton stored
public class ServiceCollector(IServiceProvider services) : IDisposable
{
    readonly List<object> instances = new();


    public IEnumerable<IEventHandler<TEvent>> GetEventHandlers<TEvent>() where TEvent : IEvent
    {
        var handlers = services
            .GetServices(typeof(IEventHandler<TEvent>))
            .OfType<IEventHandler<TEvent>>()
            .ToList();

        lock (this.instances)
        {
            var handlerInstances = this.instances.OfType<IEventHandler<TEvent>>().ToList();
            if (handlerInstances.Count > 0)
                handlers.AddRange(handlerInstances);
        }

        // services.GetServices
        // this.instances.Get
        return null;
    }
    
    public void AddInstance(object instance)
    {
        lock (this.instances)
        {
            // TODO: log if in already?
            this.instances.Add(instance);
        }
    }


    public void RemoveInstance(object instance)
    {
        lock (this.instances)
            
            // TODO: log if missing
            this.instances.Remove(instance);
        }        
    }


    public void Dispose()
    {
        this.instances.Clear();
    }
}