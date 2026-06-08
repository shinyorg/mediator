namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Wire envelope persisted by <see cref="StorageCacheService"/>. The cached value is stored as a
/// pre-serialized JSON string so the storage layer only needs to know how to serialize this single
/// non-generic record — eliminating the need for <c>InternalCacheEntry&lt;T&gt;</c> registrations
/// per response type. The two-phase pattern mirrors <see cref="OfflineStore"/>.
/// </summary>
public record InternalCacheEntry(
    string Key,
    string ValueJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    CacheItemConfig? Config
);


/// <summary>
/// <see cref="ICacheService"/> implementation that persists entries to an
/// <see cref="IStorageService"/> (typically a file-backed store). All operations
/// against a single key are serialized via an internal <see cref="KeyedLocker"/>
/// so that the factory in <see cref="GetOrCreate{T}"/> runs exactly once and
/// sliding-expiration writes performed during reads cannot race.
/// </summary>
public class StorageCacheService(
    IStorageService storage,
    Shiny.ISerializer serializer,
    TimeProvider timeProvider
) : ICacheService
{
    /// <summary>The storage category under which cache entries are written.</summary>
    public const string Category = "Cache";

    /// <summary>Default cache configuration applied when none is supplied (10 minute absolute expiration).</summary>
    public static CacheItemConfig DefaultCache = new CacheItemConfig
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(10)
    };

    readonly KeyedLocker locker = new();

    /// <inheritdoc/>
    public async Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> retrieveFunc, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
    {
        using (await this.locker.LockAsync(key, cancellationToken).ConfigureAwait(false))
        {
            var hit = await this.TryGet<T>(key, cancellationToken).ConfigureAwait(false);
            if (hit != null)
                return hit;

            var result = await retrieveFunc
                .Invoke()
                .ConfigureAwait(false);

            return await this
                .StoreAndReturn(key, result, config, cancellationToken)
                .ConfigureAwait(false);
        }
    }


    /// <inheritdoc/>
    public async Task<CacheEntry<T>> Set<T>(string key, T value, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
    {
        using (await this.locker.LockAsync(key, cancellationToken).ConfigureAwait(false))
        {
            var entry = await this
                .StoreAndReturn(key, value, config, cancellationToken)
                .ConfigureAwait(false);

            return entry!;
        }
    }


    /// <inheritdoc/>
    public async Task<CacheEntry<T>?> Get<T>(string key, CancellationToken cancellationToken)
    {
        using (await this.locker.LockAsync(key, cancellationToken).ConfigureAwait(false))
        {
            return await this
                .TryGet<T>(key, cancellationToken)
                .ConfigureAwait(false);
        }
    }


    /// <inheritdoc/>
    public Task Remove(string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
        => storage.Remove(Category, requestKey, partialMatch, cancellationToken);


    /// <inheritdoc/>
    public Task Clear(CancellationToken cancellationToken) => storage.Clear(Category, cancellationToken);


    async Task<CacheEntry<T>?> TryGet<T>(string key, CancellationToken cancellationToken)
    {
        var envelope = await storage
            .Get<InternalCacheEntry>(Category, key, cancellationToken)
            .ConfigureAwait(false);

        if (envelope is null)
            return null;

        var now = timeProvider.GetUtcNow();

        if (envelope.ExpiresAt is not null && envelope.ExpiresAt < now)
        {
            await storage.Remove(Category, envelope.Key, false, cancellationToken).ConfigureAwait(false);
            return null;
        }

        if (envelope.Config?.SlidingExpiration is not null)
        {
            var expiresAt = now.Add(envelope.Config.SlidingExpiration.Value);
            envelope = envelope with { ExpiresAt = expiresAt };
            await storage.Set(Category, envelope.Key, envelope, cancellationToken).ConfigureAwait(false);
        }

        var value = serializer.Deserialize<T>(envelope.ValueJson);
        return new CacheEntry<T>(envelope.Key, value, envelope.CreatedAt);
    }


    async Task<CacheEntry<T>> StoreAndReturn<T>(string key, T result, CacheItemConfig? config, CancellationToken cancellationToken)
    {
        DateTimeOffset? expiresAt = null;
        var now = timeProvider.GetUtcNow();
        var effectiveConfig = config ?? DefaultCache;

        if (effectiveConfig.AbsoluteExpiration is not null)
            expiresAt = now.Add(effectiveConfig.AbsoluteExpiration.Value);
        else if (effectiveConfig.SlidingExpiration is not null)
            expiresAt = now.Add(effectiveConfig.SlidingExpiration.Value);

        // Pre-serialize the value to JSON so the persisted envelope (InternalCacheEntry) is
        // non-generic — the storage layer never sees the closed-generic shape of the cached value
        // and the resolver only needs the user's TResponse registered (which the mediator
        // generator already does).
        var valueJson = serializer.Serialize(result);
        var envelope = new InternalCacheEntry(key, valueJson, now, expiresAt, effectiveConfig);

        await storage.Set(Category, key, envelope, cancellationToken).ConfigureAwait(false);

        return new CacheEntry<T>(key, result, now);
    }
}
