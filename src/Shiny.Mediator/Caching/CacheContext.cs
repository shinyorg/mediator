using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching;

/// <summary>
/// Describes the cache outcome for a request: the resolved key, whether the value came from cache,
/// the entry's creation timestamp, and the configuration that was applied.
/// </summary>
/// <param name="RequestKey">The contract key used for the cache lookup.</param>
/// <param name="IsHit">True if the value was served from cache; false if the handler executed.</param>
/// <param name="Timestamp">UTC time the cache entry was created.</param>
/// <param name="Config">The cache configuration applied for this request, if any.</param>
public record CacheContext(
    string RequestKey,
    bool IsHit,
    DateTimeOffset Timestamp,
    CacheItemConfig? Config = null
);