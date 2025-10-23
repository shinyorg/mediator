using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public sealed class ShinyMediatorBuilder(IServiceCollection services)
{
    public IServiceCollection Services => services;


    public ShinyMediatorBuilder SetContractKeyProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>() where TProvider : class, IContractKeyProvider
    {
        this.Services.AddSingleton<IContractKeyProvider, TProvider>();
        return this;
    }

    
    public ShinyMediatorBuilder SetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer>() where TSerializer : class, ISerializerService
    {
        this.Services.AddSingleton<ISerializerService, TSerializer>();
        return this;
    }

    
    public ShinyMediatorBuilder AddRequestMiddleware<TRequest, TResult, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>(
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
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


    public ShinyMediatorBuilder AddOpenCommandMiddleware([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Add(new ServiceDescriptor(typeof(ICommandMiddleware<>), null, implementationType, lifetime));
        return this;
    }
    
    
    public ShinyMediatorBuilder AddOpenRequestMiddleware([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
    {
        services.Add(new ServiceDescriptor(typeof(IRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }
    
    
    public ShinyMediatorBuilder AddOpenStreamMiddleware([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
    {
        services.Add(new ServiceDescriptor(typeof(IStreamRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }
    

    public ShinyMediatorBuilder AddOpenEventMiddleware([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Add(new ServiceDescriptor(typeof(IEventMiddleware<>), null, implementationType, lifetime));
        return this;
    }
    

    public ShinyMediatorBuilder AddEventCollector<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>() where TImpl : class, IEventCollector
    {
        if (!services.Any(x => x.ServiceType == typeof(TImpl)))
            services.AddSingleton<IEventCollector, TImpl>();
        
        return this;
    }
}