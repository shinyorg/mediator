using Microsoft.Extensions.Caching.Memory;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public class MemoryCacheService(IMemoryCache cache, TimeProvider timeProvider) : ICacheService
{
    public Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> retrieveFunc, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(
            key, 
            async _ =>
            {
                var result = await retrieveFunc.Invoke().ConfigureAwait(false);
                return new CacheEntry<T>(
                    key,
                    result,
                    timeProvider.GetUtcNow()
                );
            }, 
            new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpirationRelativeToNow = config?.AbsoluteExpiration,
                SlidingExpiration = config?.SlidingExpiration
            }
        );
    

    public Task<CacheEntry<T>> Set<T>(string key, T value, CacheItemConfig? config = null, CancellationToken cancellationToken = default)
    {
        // TODO: what if entry already exists?
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
            var entries = cache.GetEntries();
            foreach (var entry in entries)
            {
                if (entry.Key is string key && key.StartsWith(requestKey))
                    cache.Remove(key); // TODO: altering enumerable
            }
        }

        return Task.CompletedTask;
    }

    public Task Clear(CancellationToken cancellationToken)
    {
        cache.Clear();
        return Task.CompletedTask;
    }
}