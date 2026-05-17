using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// Fluent builder used during startup to register handlers, middleware, serializers, contract-key providers
/// and event collectors for Shiny Mediator. Obtained from <c>AddShinyMediator</c>.
/// </summary>
public sealed class ShinyMediatorBuilder(IServiceCollection services)
{
    /// <summary>
    /// The underlying <see cref="IServiceCollection"/> being configured.
    /// </summary>
    public IServiceCollection Services => services;


    /// <summary>
    /// Replaces the registered <see cref="IContractKeyProvider"/> used to compute unique keys for cache, replay, and storage lookups.
    /// </summary>
    public ShinyMediatorBuilder SetContractKeyProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>() where TProvider : class, IContractKeyProvider
    {
        this.Services.AddSingleton<IContractKeyProvider, TProvider>();
        return this;
    }


    /// <summary>
    /// Replaces the registered <see cref="ISerializerService"/> used by mediator infrastructure (HTTP, caching, etc.).
    /// </summary>
    public ShinyMediatorBuilder SetSerializer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer
    >() where TSerializer : class, ISerializerService
    {
        this.Services.AddSingleton<ISerializerService, TSerializer>();
        return this;
    }


    /// <summary>
    /// Registers a closed-generic <see cref="IRequestMiddleware{TRequest,TResult}"/> targeting a specific request/result pair.
    /// </summary>
    /// <param name="lifetime">DI lifetime for the middleware instance.</param>
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
    /// Registers an open-generic <see cref="ICommandMiddleware{TCommand}"/> implementation that applies to every command type.
    /// </summary>
    /// <param name="implementationType">An open-generic implementation type, e.g. <c>typeof(MyMiddleware&lt;&gt;)</c>.</param>
    /// <param name="lifetime">DI lifetime for the middleware instance.</param>
    public ShinyMediatorBuilder AddOpenCommandMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        services.Add(new ServiceDescriptor(typeof(ICommandMiddleware<>), null, implementationType, lifetime));
        return this;
    }


    /// <summary>
    /// Registers an open-generic <see cref="IRequestMiddleware{TRequest,TResult}"/> implementation that applies to every request type.
    /// </summary>
    /// <param name="implementationType">An open-generic implementation type, e.g. <c>typeof(MyMiddleware&lt;,&gt;)</c>.</param>
    /// <param name="lifetime">DI lifetime for the middleware instance.</param>
    public ShinyMediatorBuilder AddOpenRequestMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        services.Add(new ServiceDescriptor(typeof(IRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }


    /// <summary>
    /// Registers an open-generic <see cref="IStreamRequestMiddleware{TRequest,TResult}"/> implementation that applies to every stream request type.
    /// </summary>
    /// <param name="implementationType">An open-generic implementation type, e.g. <c>typeof(MyMiddleware&lt;,&gt;)</c>.</param>
    /// <param name="lifetime">DI lifetime for the middleware instance.</param>
    public ShinyMediatorBuilder AddOpenStreamMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        services.Add(new ServiceDescriptor(typeof(IStreamRequestMiddleware<,>), null, implementationType, lifetime));
        return this;
    }


    /// <summary>
    /// Registers an open-generic <see cref="IEventMiddleware{TEvent}"/> implementation that applies to every event type.
    /// </summary>
    /// <param name="implementationType">An open-generic implementation type, e.g. <c>typeof(MyMiddleware&lt;&gt;)</c>.</param>
    /// <param name="lifetime">DI lifetime for the middleware instance.</param>
    public ShinyMediatorBuilder AddOpenEventMiddleware(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        services.Add(new ServiceDescriptor(typeof(IEventMiddleware<>), null, implementationType, lifetime));
        return this;
    }


    /// <summary>
    /// Registers an <see cref="IEventCollector"/> that contributes additional event handlers at publish-time
    /// (for example handlers held outside DI). No-op if the type is already registered.
    /// </summary>
    public ShinyMediatorBuilder AddEventCollector<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl
    >() where TImpl : class, IEventCollector
    {
        if (!services.Any(x => x.ServiceType == typeof(TImpl)))
            services.AddSingleton<IEventCollector, TImpl>();

        return this;
    }
}
