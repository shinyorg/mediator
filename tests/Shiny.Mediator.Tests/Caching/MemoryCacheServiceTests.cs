using DryIoc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Time.Testing;

namespace Shiny.Mediator.Tests.Caching;

public class MemoryCacheServiceTests
{
    [Fact]
    public async Task EndToEndTest()
    {
        var fake = new FakeTimeProvider();
        var memCache = new MemoryCache(new MemoryCacheOptions());
        
        var service = new MemoryCacheService(memCache, fake);
        var result = await service.GetOrCreate("test", () => Task.FromResult(fake.GetTimestamp()));
        
        result.ShouldNotBeNull();
        var secondResult = await service.GetOrCreate("test", () => Task.FromResult(fake.GetTimestamp()));
        
        secondResult.ShouldNotBeNull();
        result.Value.ShouldBe(secondResult.Value); // should have retrieved from cache
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