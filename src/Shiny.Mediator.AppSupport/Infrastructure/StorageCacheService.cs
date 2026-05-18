namespace Shiny.Mediator.Infrastructure;


record InternalCacheEntry<T>(
    string Key,
    T Value,
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
            var e = await this.TryGet<T>(key, cancellationToken).ConfigureAwait(false);
            if (e == null)
            {
                var result = await retrieveFunc
                    .Invoke()
                    .ConfigureAwait(false);

                e = await this
                    .Store(key, result, config, cancellationToken)
                    .ConfigureAwait(false);
            }
            return ToExternal(e);
        }
    }


    /// <inheritdoc/>
    public async Task<CacheEntry<T>> Set<T>(string key, T value, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
    {
        using (await this.locker.LockAsync(key, cancellationToken).ConfigureAwait(false))
        {
            var intCache = await this.Store(key, value, config, cancellationToken).ConfigureAwait(false);
            var entry = ToExternal(intCache);
            return entry!;
        }
    }


    /// <inheritdoc/>
    public async Task<CacheEntry<T>?> Get<T>(string key, CancellationToken cancellationToken)
    {
        using (await this.locker.LockAsync(key, cancellationToken).ConfigureAwait(false))
        {
            var e = await this
                .TryGet<T>(key, cancellationToken)
                .ConfigureAwait(false);

            return ToExternal(e);
        }
    }


    /// <inheritdoc/>
    public Task Remove(string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
        => storage.Remove(Category, requestKey, partialMatch, cancellationToken);


    /// <inheritdoc/>
    public Task Clear(CancellationToken cancellationToken) => storage.Clear(Category, cancellationToken);


    static CacheEntry<T>? ToExternal<T>(InternalCacheEntry<T>? e)
    {
        if (e == null)
            return null;

        return new(e.Key, e.Value, e.CreatedAt);
    }

    async Task<InternalCacheEntry<T>?> TryGet<T>(string key, CancellationToken cancellationToken)
    {
        var e = await storage
            .Get<InternalCacheEntry<T>>(Category, key, cancellationToken)
            .ConfigureAwait(false);

        if (e != null)
        {
            var now = timeProvider.GetUtcNow();

            if (e.ExpiresAt != null && e.ExpiresAt < now)
            {
                await storage.Remove(Category, e.Key, false, cancellationToken).ConfigureAwait(false);
                e = null;
            }
            else if (e.Config?.SlidingExpiration != null)
            {
                var expiresAt = now.Add(e.Config.SlidingExpiration.Value);
                e = e with { ExpiresAt = expiresAt };
                await storage.Set(Category, e.Key, e, cancellationToken).ConfigureAwait(false);
            }
        }

        return e;
    }

    async Task<InternalCacheEntry<T>> Store<T>(string key, T result, CacheItemConfig? config, CancellationToken cancellationToken)
    {
        DateTimeOffset? expiresAt = null;
        var now = timeProvider.GetUtcNow();

        if (config != null)
        {
            if (config.AbsoluteExpiration != null)
            {
                expiresAt = now.Add(config.AbsoluteExpiration.Value);
            }
            else if (config.SlidingExpiration != null)
            {
                expiresAt = now.Add(config.SlidingExpiration.Value);
            }
        }
        var e = new InternalCacheEntry<T>(
            key,
            result,
            now,
            expiresAt,
            config ?? DefaultCache
        );
        await storage.Set(Category, key, e, cancellationToken).ConfigureAwait(false);

        return e;
    }
}
