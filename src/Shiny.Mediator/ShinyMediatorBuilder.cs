using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public sealed class ShinyMediatorBuilder(IServiceCollection services)
{
    public IServiceCollection Services => services;


    public ShinyMediatorBuilder SetSerializer<TSerializer>() where TSerializer : class, ISerializerService
    {
        this.Services.AddSingleton<ISerializerService, TSerializer>();
        return this;
    }

    
    public ShinyMediatorBuilder AddRequestMiddleware<TRequest, TResult, TImpl>(
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


    public ShinyMediatorBuilder AddOpenCommandMiddleware(Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Add(new ServiceDescriptor(typeof(ICommandMiddleware<>), null, implementationType, lifetime));
        return this;
    }
    
    
    public ShinyMediatorBuilder AddOpenRequestMiddleware(Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
    {
        services.Add(new ServiceDescriptor(typeof(IRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }
    
    
    public ShinyMediatorBuilder AddOpenStreamMiddleware(Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
    {
        services.Add(new ServiceDescriptor(typeof(IStreamRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }
    

    public ShinyMediatorBuilder AddOpenEventMiddleware(Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Add(new ServiceDescriptor(typeof(IEventMiddleware<>), null, implementationType, lifetime));
        return this;
    }
    

    public ShinyMediatorBuilder AddEventCollector<TImpl>() where TImpl : class, IEventCollector
    {
        if (!services.Any(x => x.ServiceType == typeof(TImpl)))
            services.AddSingleton<IEventCollector, TImpl>();
        
        return this;
    }
}