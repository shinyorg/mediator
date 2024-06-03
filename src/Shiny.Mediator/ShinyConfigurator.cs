using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;


public sealed class ShinyConfigurator(IServiceCollection services)
{
    public ShinyConfigurator AddRequestMiddleware<TRequest, TResult, TImpl>(
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TRequest : IRequest<TResult>
        where TImpl : class, IRequestMiddleware<TRequest, TResult>
    {
        switch (lifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient<IRequestMiddleware<TRequest, TResult>, TImpl>();
                break;
            
            case ServiceLifetime.Scoped:
                services.AddScoped<IRequestMiddleware<TRequest, TResult>, TImpl>();
                break;
            
            case ServiceLifetime.Singleton:
                services.AddSingleton<IRequestMiddleware<TRequest, TResult>, TImpl>();
                break;
        }

        return this;
    }
    
    
    public ShinyConfigurator AddOpenRequestMiddleware(Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
    {
        // validate open generic
        services.Add(new ServiceDescriptor(typeof(IRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }
   

    public ShinyConfigurator AddEventCollector<TImpl>() where TImpl : class, IEventCollector
    {
        services.AddSingleton<IEventCollector, TImpl>();
        return this;
    }
}