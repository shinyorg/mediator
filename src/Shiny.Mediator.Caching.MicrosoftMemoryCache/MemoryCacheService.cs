using Microsoft.Extensions.Caching.Memory;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public class MemoryCacheService(IMemoryCache cache, TimeProvider timeProvider) : ICacheService
{
    readonly KeyedLocker locker = new();

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
            cache.Set(key, entry, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpirationRelativeToNow = config?.AbsoluteExpiration,
                SlidingExpiration = config?.SlidingExpiration
            });
            return entry;
        }
    }


    public Task<CacheEntry<T>> Set<T>(string key, T value, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
    {
        var entryValue = new CacheEntry<T>(key, value, timeProvider.GetUtcNow());
        var opts = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = config?.AbsoluteExpiration,
            SlidingExpiration = config?.SlidingExpiration
        };
        cache.Set(key, entryValue, opts);

        return Task.FromResult(entryValue);
    }


    public Task<CacheEntry<T>?> Get<T>(string key, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out var result) && result is CacheEntry<T> entry)
            return Task.FromResult(entry)!;

        return Task.FromResult<CacheEntry<T>?>(null);
    }


    public Task Remove(string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
    {
        if (!partialMatch)
        {
            cache.Remove(requestKey);
        }
        else
        {
            var keysToRemove = cache
                .GetEntries()
                .Select(e => e.Key)
                .OfType<string>()
                .Where(k => k.StartsWith(requestKey))
                .ToList();

            foreach (var key in keysToRemove)
                cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    public Task Clear(CancellationToken cancellationToken)
    {
        cache.Clear();
        return Task.CompletedTask;
    }
}
