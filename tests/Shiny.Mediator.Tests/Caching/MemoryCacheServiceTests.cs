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
    public async Task GetSet_SimpleType_Test()
    {
        var fake = new FakeTimeProvider();
        var memCache = new MemoryCache(new MemoryCacheOptions());
        
        var service = new MemoryCacheService(memCache, fake);
        var result = await service.Set("test", fake.GetTimestamp());
        
        result.ShouldNotBeNull();
        var secondResult = await service.Get<long>("test");
        
        secondResult.ShouldNotBeNull();
        result.Value.ShouldBe(secondResult.Value);
    }
    

    [Fact]
    public async Task GetSet_ReferenceType_Test()
    {
        var fake = new FakeTimeProvider();
        var memCache = new MemoryCache(new MemoryCacheOptions());
        
        var obj = new MyClass { Id = 123, Name = "Hello World" };
        var service = new MemoryCacheService(memCache, fake);
        var result = await service.Set("myobj", obj);
        result.ShouldNotBeNull();
        
        var secondResult = await service.Get<MyClass>("myobj");
        secondResult.ShouldNotBeNull();
        result.Value.Id.ShouldBe(secondResult.Value.Id);
        result.Value.Name.ShouldBe(secondResult.Value.Name);
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

file class MyClass
{
    public int Id { get; set; }
    public string Name { get; set; }
}