using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Mocks;


public class MockCacheService(FakeTimeProvider timeProvider) : ICacheService
{
    public Dictionary<string, object> Items { get; } = new();
    
    public async Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> factory, CacheItemConfig? config = null)
    {
        var item = await this.Get<T>(key);
        if (item == null)
        {
            var value = await factory.Invoke();
            await this.Set(key, value, config);
            item = await this.Get<T>(key);
        }

        return item;
    }
    

    public Task Set<T>(string key, T value, CacheItemConfig? config = null)
    {
        this.Items[key] = new CacheEntry<T>(key, value, timeProvider.GetUtcNow());
        return Task.CompletedTask;
    }

    
    public Task<CacheEntry<T>?> Get<T>(string key)
    {
        CacheEntry<T>? entry = null;
        if (this.Items.ContainsKey(key))
            entry = this.Items[key] as CacheEntry<T>;

        return Task.FromResult(entry);
    }

    public Task Remove(string requestKey, bool partialMatch = false)
    {
        // TODO: implement partialMatch
        this.Items.Remove(requestKey);
        return Task.CompletedTask;
    }

    public Task Clear()
    {
        this.Items.Clear();
        return Task.CompletedTask;
    }
}