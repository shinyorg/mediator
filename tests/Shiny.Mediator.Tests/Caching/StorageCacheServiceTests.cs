using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Tests.Mocks;

namespace Shiny.Mediator.Tests.Caching;


public class StorageCacheServiceTests : BaseCacheServiceTests
{
    protected override ICacheService CreateService(FakeTimeProvider timeProvider)
    {
        var fakeStore = new MockStorageService();
        var cache = new StorageCacheService(fakeStore, TestHelpers.CreateTestSerializer(), timeProvider);
        return cache;
    }


    [Fact(DisplayName = "Storage layer receives an InternalCacheEntry envelope, not the raw T")]
    public async Task Set_StoresEnvelope_NotRawValue()
    {
        var storage = new MockStorageService();
        var serializer = TestHelpers.CreateTestSerializer();
        var cache = new StorageCacheService(storage, serializer, new FakeTimeProvider(new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)));

        var input = new EnvelopeTestPayload(42, "hello");
        await cache.Set("payload", input);

        // What landed in storage must be an InternalCacheEntry — proves the two-phase wrapper is
        // genuinely applied and a future regression that bypasses it would fail here loudly.
        storage.Items.ShouldContainKey(StorageCacheService.Category);
        var raw = storage.Items[StorageCacheService.Category]["payload"];
        var envelope = raw.ShouldBeOfType<InternalCacheEntry>();

        envelope.Key.ShouldBe("payload");
        envelope.ValueJson.ShouldNotBeNullOrWhiteSpace();

        // And the pre-serialized JSON inside the envelope must parse back to the original T —
        // proves it's the value's wire format, not stringified metadata or a leaked toString().
        var parsed = serializer.Deserialize<EnvelopeTestPayload>(envelope.ValueJson);
        parsed.ShouldNotBeNull();
        parsed!.Id.ShouldBe(42);
        parsed.Name.ShouldBe("hello");
    }


    public record EnvelopeTestPayload(int Id, string Name);
}
