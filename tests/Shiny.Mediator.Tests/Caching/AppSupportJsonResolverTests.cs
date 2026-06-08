using System.Text.Json;
using Shiny.Impl;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Caching;


/// <summary>
/// Locks the on-disk wire format of the three envelope types Shiny.Mediator.AppSupport ships
/// converters for. A future refactor that silently changes the JSON shape would break cached
/// entries from older app versions — these tests are the gate.
/// </summary>
public class AppSupportJsonResolverTests
{
    static Shiny.ISerializer CreateStrictSerializer()
    {
        // Fresh serializer with ONLY the AppSupportJsonResolver on the chain — no reflection
        // fallback. Proves the resolver alone is sufficient for the envelope types.
        var s = new DefaultJsonSerializer();
        s.Options.TypeInfoResolverChain.Add(AppSupportJsonResolver.Instance);
        return s;
    }


    [Fact(DisplayName = "InternalCacheEntry round-trips via AppSupportJsonResolver")]
    public void InternalCacheEntry_RoundTrips()
    {
        var serializer = CreateStrictSerializer();
        var input = new InternalCacheEntry(
            Key: "rides:current",
            ValueJson: "[{\"Id\":\"a\"}]",
            CreatedAt: new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero),
            ExpiresAt: new DateTimeOffset(2026, 6, 7, 12, 10, 0, TimeSpan.Zero),
            Config: new CacheItemConfig(AbsoluteExpiration: TimeSpan.FromMinutes(10), SlidingExpiration: null)
        );

        var json = serializer.Serialize(input);
        var back = serializer.Deserialize<InternalCacheEntry>(json);

        back.ShouldNotBeNull();
        back.Key.ShouldBe("rides:current");
        back.ValueJson.ShouldBe("[{\"Id\":\"a\"}]");
        back.CreatedAt.ShouldBe(input.CreatedAt);
        back.ExpiresAt.ShouldBe(input.ExpiresAt);
        back.Config.ShouldNotBeNull();
        back.Config!.AbsoluteExpiration.ShouldBe(TimeSpan.FromMinutes(10));
        back.Config.SlidingExpiration.ShouldBeNull();
    }


    [Fact(DisplayName = "InternalCacheEntry round-trips with null ExpiresAt and null Config")]
    public void InternalCacheEntry_AllOptionalNull_RoundTrips()
    {
        var serializer = CreateStrictSerializer();
        var input = new InternalCacheEntry(
            Key: "k",
            ValueJson: "\"v\"",
            CreatedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ExpiresAt: null,
            Config: null
        );

        var json = serializer.Serialize(input);
        var back = serializer.Deserialize<InternalCacheEntry>(json);

        back.ShouldNotBeNull();
        back.ExpiresAt.ShouldBeNull();
        back.Config.ShouldBeNull();
    }


    [Theory(DisplayName = "CacheItemConfig nullable matrix round-trips")]
    [InlineData(true, true)]   // both set
    [InlineData(true, false)]  // absolute only
    [InlineData(false, true)]  // sliding only
    [InlineData(false, false)] // both null
    public void CacheItemConfig_NullableMatrix_RoundTrips(bool absoluteSet, bool slidingSet)
    {
        var serializer = CreateStrictSerializer();
        var input = new CacheItemConfig(
            AbsoluteExpiration: absoluteSet ? TimeSpan.FromMinutes(15) : null,
            SlidingExpiration: slidingSet ? TimeSpan.FromMinutes(3) : null
        );

        var json = serializer.Serialize(input);
        var back = serializer.Deserialize<CacheItemConfig>(json);

        back.ShouldNotBeNull();
        back.AbsoluteExpiration.ShouldBe(input.AbsoluteExpiration);
        back.SlidingExpiration.ShouldBe(input.SlidingExpiration);
    }


    [Fact(DisplayName = "OfflineStore round-trips via AppSupportJsonResolver")]
    public void OfflineStore_RoundTrips()
    {
        var serializer = CreateStrictSerializer();
        var input = new OfflineStore(
            TypeName: "MyApp.GetThing",
            RequestKey: "GetThing_42",
            Timestamp: new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero),
            Json: "{\"id\":42}"
        );

        var json = serializer.Serialize(input);
        var back = serializer.Deserialize<OfflineStore>(json);

        back.ShouldNotBeNull();
        back.TypeName.ShouldBe(input.TypeName);
        back.RequestKey.ShouldBe(input.RequestKey);
        back.Timestamp.ShouldBe(input.Timestamp);
        back.Json.ShouldBe(input.Json);
    }


    [Fact(DisplayName = "Resolver returns null for unknown types (composes in chain)")]
    public void Resolver_ReturnsNullForUnknownTypes()
    {
        var options = new JsonSerializerOptions();
        var typeInfo = AppSupportJsonResolver.Instance.GetTypeInfo(typeof(int), options);
        typeInfo.ShouldBeNull();
    }
}
