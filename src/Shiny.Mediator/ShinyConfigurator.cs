using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public sealed class ShinyConfigurator(IServiceCollection services)
{
    public IServiceCollection Services => services;

    public ShinyConfigurator SetRequestSender<TRequestSender>() where TRequestSender : class, IRequestSender
    {
        this.Services.AddSingleton<IRequestSender, TRequestSender>();
        return this;
    }
    
    
    public ShinyConfigurator SetEventPublisher<TEventPublisher>() where TEventPublisher : class, IEventPublisher
    {
        this.Services.AddSingleton<IEventPublisher, TEventPublisher>();
        return this;
    }
    
    
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
        // TODO: validate open generic
        services.Add(new ServiceDescriptor(typeof(IRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }


    public ShinyConfigurator AddOpenEventMiddleware(Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        // TODO: validate open generic
        services.Add(new ServiceDescriptor(typeof(IEventMiddleware<>), null, implementationType, lifetime));
        return this;
    }

    public ShinyConfigurator AddEventCollector<TImpl>() where TImpl : class, IEventCollector
    {
        services.AddSingleton<IEventCollector, TImpl>();
        return this;
    }
}