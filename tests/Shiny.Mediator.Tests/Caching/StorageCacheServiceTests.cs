using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Tests.Mocks;

namespace Shiny.Mediator.Tests.Caching;


public class StorageCacheServiceTests : BaseCacheServiceTests
{
    protected override ICacheService CreateService(FakeTimeProvider timeProvider)
    {
        var fakeStore = new MockStorageService();
        var cache = new StorageCacheService(fakeStore, timeProvider);
        return cache;
    }
}