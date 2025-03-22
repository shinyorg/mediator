using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Tests.Mocks;

namespace Shiny.Mediator.Tests.Caching;


public class StorageCacheServiceTests
{
    readonly FakeTimeProvider fakeTime;
    readonly MockStorageService fakeStore;
    readonly StorageCacheService cache;
    
    
    public StorageCacheServiceTests() 
    {
        this.fakeTime = new FakeTimeProvider(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
        this.fakeStore = new MockStorageService();
        this.cache = new StorageCacheService(this.fakeStore, this.fakeTime);
    }


    [Fact]
    public async Task GetOrCreate_MustCreate()
    {
        var item = await this.cache.GetOrCreate(
            "test",
            async () => "notfromcache"
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("notfromcache");
        
    }


    [Fact]
    public async Task GetOrCreate_MustGet()
    {
        await this.cache.Set("test", "fromcache");
        var item = await this.cache.GetOrCreate(
            "test",
            async () => "notfromcache"
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("fromcache");
    }
    

    [Fact]
    public async Task GetOrCreate_ShouldReturn_NotFromCache_Expired()
    {
        var cfg = new CacheItemConfig
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(1)
        };
        
        var item = await this.cache.GetOrCreate(
            "test",
            async () => "notfromcache",
            cfg
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("notfromcache");

        this.fakeTime.Advance(TimeSpan.FromMinutes(2));
        item = await this.cache.GetOrCreate(
            "test",
            async () => "notfromcache",
            cfg
        );
        item.ShouldNotBeNull();
        item.Value.ShouldBe("notfromcache");

    }
}