using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

/// <summary>
/// Event published to instruct every store/cache participant to clear all entries.
/// </summary>
public record FlushAllStoresEvent : IEvent;

/// <summary>
/// Event published to instruct every store/cache participant to remove entries matching <see cref="RequestKey"/>.
/// </summary>
/// <param name="RequestKey">The contract key (or prefix) to flush.</param>
/// <param name="PartialMatch">When true, treat <see cref="RequestKey"/> as a prefix; when false, perform an exact-match removal.</param>
public record FlushStoresEvent(string RequestKey, bool PartialMatch) : IEvent;

/// <summary>
/// Helpers for publishing <see cref="FlushAllStoresEvent"/> and <see cref="FlushStoresEvent"/> events and for
/// clearing cache entries scoped to a particular request.
/// </summary>
public static class FlushStoreExtensions
{
    /// <summary>
    /// Publishes a <see cref="FlushAllStoresEvent"/> so every cache and store middleware clears its contents.
    /// </summary>
    public static Task FlushAllStores(this IMediator mediator, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushAllStoresEvent(), cancellationToken);

    /// <summary>
    /// Publishes a <see cref="FlushAllStoresEvent"/> from an existing context, reusing its scope.
    /// </summary>
    public static Task FlushAllStores(this IMediatorContext context, CancellationToken cancellationToken = default)
        => context.Publish(new FlushAllStoresEvent(), true, cancellationToken);

    /// <summary>
    /// Publishes a <see cref="FlushStoresEvent"/> to remove cache entries for a specific key (or prefix).
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="requestKey">The exact key or key prefix to remove.</param>
    /// <param name="partialMatch">When true, removes all entries whose key starts with <paramref name="requestKey"/>.</param>
    /// <param name="cancellationToken"></param>
    public static Task FlushStores(this IMediator mediator, string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushStoresEvent(requestKey, partialMatch), cancellationToken);


    /// <summary>
    /// Publishes a <see cref="FlushStoresEvent"/> from an existing context, removing cache entries for a specific key (or prefix).
    /// </summary>
    /// <param name="context"></param>
    /// <param name="requestKey">The exact key or key prefix to remove.</param>
    /// <param name="partialMatch">When true, removes all entries whose key starts with <paramref name="requestKey"/>.</param>
    /// <param name="cancellationToken"></param>
    public static Task FlushStores(this IMediatorContext context, string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
        => context.Publish(new FlushStoresEvent(requestKey, partialMatch), true, cancellationToken);


    /// <summary>
    /// Flushes cache entries keyed for the current context's message. The contract key is resolved through
    /// the registered <see cref="IContractKeyProvider"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="partialKeyMatch">When true, removes all entries whose key starts with the resolved contract key.</param>
    public static Task ClearCache(this IMediatorContext context, bool partialKeyMatch = false)
    {
        var contractKeyProvider = context.ServiceScope.ServiceProvider.GetRequiredService<IContractKeyProvider>();
        var contractKey = contractKeyProvider.GetContractKey(context.Message);

        return context.FlushStores(contractKey, partialKeyMatch);
    }
}
