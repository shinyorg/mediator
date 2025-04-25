using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Caching;


public class MemoryCacheServiceTests : BaseCacheServiceTests
{

    protected override ICacheService CreateService(FakeTimeProvider timeProvider)
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var service = new MemoryCacheService(memCache, timeProvider);
        return service;
    }
    

    [Fact]
    public void MemoryCache_Clear()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        memCache.Set("test", "value");
        memCache.Get("test").ShouldBe("value");
        memCache.Clear();
        memCache.Get("test").ShouldBeNull();
    }


    [Fact]
    public void MemoryCache_GetEntries()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        memCache.Set("test", "value");
        
        var entries = memCache.GetEntries();
        entries.ShouldNotBeNull();
        entries.Count.ShouldBe(1);
    }
}