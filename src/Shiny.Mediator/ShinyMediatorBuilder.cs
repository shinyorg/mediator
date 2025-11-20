using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public sealed class ShinyMediatorBuilder(IServiceCollection services)
{
    public IServiceCollection Services => services;


    /// <summary>
    /// Sets the contract key provider
    /// </summary>
    /// <typeparam name="TProvider"></typeparam>
    /// <returns></returns>
    public ShinyMediatorBuilder SetContractKeyProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>() where TProvider : class, IContractKeyProvider
    {
        this.Services.AddSingleton<IContractKeyProvider, TProvider>();
        return this;
    }

    
    /// <summary>
    /// Sets the serializer service
    /// </summary>
    /// <typeparam name="TSerializer"></typeparam>
    /// <returns></returns>
    public ShinyMediatorBuilder SetSerializer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer
    >() where TSerializer : class, ISerializerService
    {
        this.Services.AddSingleton<ISerializerService, TSerializer>();
        return this;
    }


    /// <summary>
    /// Registers a command middleware
    /// </summary>
    /// <param name="lifetime"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public ShinyMediatorBuilder AddRequestMiddleware<
        TRequest, 
        TResult, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl
    >(
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


    /// <summary>
    /// Registers an open generic command middleware
    /// </summary>
    /// <param name="implementationType"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public ShinyMediatorBuilder AddOpenCommandMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        services.Add(new ServiceDescriptor(typeof(ICommandMiddleware<>), null, implementationType, lifetime));
        return this;
    }
    
    
    /// <summary>
    /// Registers an open generic request middleware
    /// </summary>
    /// <param name="implementationType"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public ShinyMediatorBuilder AddOpenRequestMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    ) 
    {
        services.Add(new ServiceDescriptor(typeof(IRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }
    
    
    /// <summary>
    /// Registers an open generic stream request middleware
    /// </summary>
    /// <param name="implementationType"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public ShinyMediatorBuilder AddOpenStreamMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    ) 
    {
        services.Add(new ServiceDescriptor(typeof(IStreamRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }
    

    /// <summary>
    /// Registers an open generic event middleware
    /// </summary>
    /// <param name="implementationType"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public ShinyMediatorBuilder AddOpenEventMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        services.Add(new ServiceDescriptor(typeof(IEventMiddleware<>), null, implementationType, lifetime));
        return this;
    }
    

    /// <summary>
    /// Registers an event collector
    /// </summary>
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public ShinyMediatorBuilder AddEventCollector<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl
    >() where TImpl : class, IEventCollector
    {
        if (!services.Any(x => x.ServiceType == typeof(TImpl)))
            services.AddSingleton<IEventCollector, TImpl>();
        
        return this;
    }
}