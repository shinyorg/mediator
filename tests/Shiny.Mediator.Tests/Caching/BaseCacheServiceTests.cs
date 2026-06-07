using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Caching;


public abstract class BaseCacheServiceTests
{
    protected FakeTimeProvider FakeTimeProvider { get; set; } =
        new(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));

    protected abstract ICacheService CreateService(FakeTimeProvider timeProvider);
    
    
    [Fact]
    public async Task GetOrCreate_MustCreate()
    {
        var cache = this.CreateService(this.FakeTimeProvider);
        var item = await cache.GetOrCreate(
            "test",
            async () => "notfromcache"
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("notfromcache");
    }


    [Fact]
    public async Task GetSet_SimpleType_Tests()
    {
        var cache = this.CreateService(this.FakeTimeProvider);
        var result = await cache.GetOrCreate("test", () => Task.FromResult(this.FakeTimeProvider.GetTimestamp()));
        
        result.ShouldNotBeNull();
        var secondResult = await cache.GetOrCreate("test", () => Task.FromResult(this.FakeTimeProvider.GetTimestamp()));
        
        secondResult.ShouldNotBeNull();
        result.Value.ShouldBe(secondResult.Value); // should have retrieved from cache
    }

    
    [Fact]
    public async Task GetSet_ReferenceType_Test()
    {
        var cache = this.CreateService(this.FakeTimeProvider);
        var obj = new MyClass { Id = 123, Name = "Hello World" };
        var result = await cache.Set("myobj", obj);
        result.ShouldNotBeNull();
        
        var secondResult = await cache.Get<MyClass>("myobj", CancellationToken.None);
        secondResult.ShouldNotBeNull();
        result.Value.Id.ShouldBe(secondResult.Value.Id);
        result.Value.Name.ShouldBe(secondResult.Value.Name);
    }
    

    [Fact]
    public async Task GetOrCreate_MustGet()
    {
        var cache = this.CreateService(this.FakeTimeProvider);
        await cache.Set("test", "fromcache");
        var item = await cache.GetOrCreate(
            "test",
            async () => "notfromcache"
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("fromcache");
    }
    

    [Fact]
    public async Task Remove_ExactKey_RemovesOnlyThatEntry()
    {
        var cache = this.CreateService(this.FakeTimeProvider);
        await cache.Set("a", "1");
        await cache.Set("b", "2");

        await cache.Remove("a");

        (await cache.Get<string>("a", CancellationToken.None)).ShouldBeNull();
        (await cache.Get<string>("b", CancellationToken.None))!.Value.ShouldBe("2");
    }


    [Fact]
    public async Task Remove_PartialMatch_RemovesPrefixedEntriesOnly()
    {
        var cache = this.CreateService(this.FakeTimeProvider);
        await cache.Set("user:1", "a");
        await cache.Set("user:2", "b");
        await cache.Set("order:1", "c");

        await cache.Remove("user:", partialMatch: true);

        (await cache.Get<string>("user:1", CancellationToken.None)).ShouldBeNull();
        (await cache.Get<string>("user:2", CancellationToken.None)).ShouldBeNull();
        (await cache.Get<string>("order:1", CancellationToken.None))!.Value.ShouldBe("c");
    }


    [Fact]
    public async Task Clear_RemovesAllEntries()
    {
        var cache = this.CreateService(this.FakeTimeProvider);
        await cache.Set("a", "1");
        await cache.Set("b", "2");

        await cache.Clear(CancellationToken.None);

        (await cache.Get<string>("a", CancellationToken.None)).ShouldBeNull();
        (await cache.Get<string>("b", CancellationToken.None)).ShouldBeNull();
    }


    [Fact]
    public async Task GetOrCreate_ShouldReturn_NotFromCache_Expired()
    {
        var cfg = new CacheItemConfig
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(1)
        };

        var cache = this.CreateService(this.FakeTimeProvider);
        var item = await cache.GetOrCreate(
            "test",
            async () => "notfromcache",
            cfg
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("notfromcache");

        this.FakeTimeProvider.Advance(TimeSpan.FromMinutes(2));
        item = await cache.GetOrCreate(
            "test",
            async () => "notfromcache",
            cfg
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("notfromcache");
    }
}

file class MyClass
{
    public int Id { get; set; }
    public string Name { get; set; }
}