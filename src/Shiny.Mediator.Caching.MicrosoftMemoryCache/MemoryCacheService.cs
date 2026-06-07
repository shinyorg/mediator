using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// <see cref="ICacheService"/> implementation backed by <see cref="IMemoryCache"/>.
/// Uses an internal per-key lock to provide cache-stampede protection on
/// <see cref="GetOrCreate{T}"/> (the underlying <c>GetOrCreateAsync</c> does not
/// serialize concurrent factory invocations on its own), and tracks issued keys
/// in a side index so prefix-removal works without reflecting over
/// <see cref="MemoryCache"/> internals.
/// </summary>
public class MemoryCacheService(IMemoryCache cache, TimeProvider timeProvider) : ICacheService
{
    readonly KeyedLocker locker = new();
    readonly ConcurrentDictionary<string, byte> keys = new();

    /// <inheritdoc/>
    public async Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> retrieveFunc, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(key, out var existing) && existing is CacheEntry<T> hit)
            return hit;

        using (await this.locker.LockAsync(key, cancellationToken).ConfigureAwait(false))
        {
            if (cache.TryGetValue(key, out existing) && existing is CacheEntry<T> hit2)
                return hit2;

            var result = await retrieveFunc.Invoke().ConfigureAwait(false);
            var entry = new CacheEntry<T>(key, result, timeProvider.GetUtcNow());
            this.Insert(key, entry, config);
            return entry;
        }
    }


    /// <inheritdoc/>
    public Task<CacheEntry<T>> Set<T>(string key, T value, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
    {
        var entryValue = new CacheEntry<T>(key, value, timeProvider.GetUtcNow());
        this.Insert(key, entryValue, config);

        return Task.FromResult(entryValue);
    }


    /// <inheritdoc/>
    public Task<CacheEntry<T>?> Get<T>(string key, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out var result) && result is CacheEntry<T> entry)
            return Task.FromResult(entry)!;

        return Task.FromResult<CacheEntry<T>?>(null);
    }


    /// <inheritdoc/>
    public Task Remove(string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
    {
        if (!partialMatch)
        {
            cache.Remove(requestKey);
        }
        else
        {
            foreach (var key in this.keys.Keys)
            {
                if (key.StartsWith(requestKey))
                    cache.Remove(key);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Clear(CancellationToken cancellationToken)
    {
        foreach (var key in this.keys.Keys)
            cache.Remove(key);

        this.keys.Clear();
        return Task.CompletedTask;
    }


    void Insert<T>(string key, CacheEntry<T> entry, CacheItemConfig? config)
    {
        var opts = new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.Normal,
            AbsoluteExpirationRelativeToNow = config?.AbsoluteExpiration,
            SlidingExpiration = config?.SlidingExpiration
        };
        opts.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
        {
            EvictionCallback = (k, _, _, _) =>
            {
                if (k is string s)
                    this.keys.TryRemove(s, out _);
            }
        });

        this.keys[key] = 0;
        cache.Set(key, entry, opts);
    }
}
