using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;


public class MediatorServiceProvider(IServiceScope services, bool scoped) : 
    IServiceProvider, 
    ISupportRequiredService, 
    IKeyedServiceProvider, 
    IServiceProviderIsKeyedService, 
    IDisposable, 
    IAsyncDisposable
{
    readonly List<object> trackEventHandlers = new();
    bool disposed;
    
    public object? GetService(Type serviceType)
    {
        var service = services.ServiceProvider.GetService(serviceType);
        this.TryTrackEventHandler(service);
        return service;
    }

    public object GetRequiredService(Type serviceType)
    {
        var service = services.ServiceProvider.GetRequiredService(serviceType);
        this.TryTrackEventHandler(service);
        return service;
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        var service = services.ServiceProvider.GetKeyedServices(serviceType, serviceKey);
        this.TryTrackEventHandler(service);
        throw new NotImplementedException();
    }
    
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => services.ServiceProvider.GetRequiredKeyedService(serviceType, serviceKey);

    public bool IsService(Type serviceType)
        => false;

    public bool IsKeyedService(Type serviceType, object? serviceKey) =>
        services.ServiceProvider is IKeyedServiceProvider;

    public IList<IEventHandler<TEvent>> GetEventHandlers<TEvent>() where TEvent : IEvent
    {
        // this is going to pull out all singletons, scoped, and transients of event handlers, BUT
            // viewmodels should not register their IEventHandler interface, it is a marker at that point
        var handlers = services
            .ServiceProvider
            .GetServices(typeof(IEventHandler<TEvent>))
            .OfType<IEventHandler<TEvent>>()
            .ToList();
    
        lock (this.trackEventHandlers)
        {
            var handlerInstances = this
                .trackEventHandlers
                .OfType<IEventHandler<TEvent>>()
                .ToList();
            
            if (handlerInstances.Count > 0)
                handlers.AddRange(handlerInstances);
        }
        return handlers;
    }
    
    void TryTrackEventHandler(object? service)
    {
        if (service == null || !scoped)
            return;

        if (IsEventHandler(service))
        {
            lock (this.trackEventHandlers)
            {
                this.trackEventHandlers.Add(service);
            }
        }
    }
    
    static bool IsEventHandler(object service) => service
        .GetType()
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(IEventHandler<>)
        );    
    
    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources;
    /// <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            this.disposed = true;
            if (disposing)
            {
                // this will call dispose on the handlers for me
                services.Dispose(); 
                
                // remove all tracked instances of IEventHandler's
                lock (this.trackEventHandlers)
                    this.trackEventHandlers.Clear();
            }
        }
    }
   
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
   
    /// <summary>
    /// Performs a dispose operation asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!this.disposed)
        {
            this.disposed = true;
            // await _lifetimeScope.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }    
}