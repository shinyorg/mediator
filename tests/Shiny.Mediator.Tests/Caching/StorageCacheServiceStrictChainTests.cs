using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shiny.Impl;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Caching;


/// <summary>
/// Reproduces the production-strict chain that failed against Wonderland in 6.6 RC:
/// <c>StorageCacheService.GetOrCreate&lt;IReadOnlyList&lt;TestEntity&gt;&gt;</c> with a serializer
/// whose chain contains ONLY <see cref="AppSupportJsonResolver"/> + a hand-rolled resolver for
/// the element type (no reflection fallback, no Json.Default pollution). If the wire format,
/// the envelope shape, or the collection wrapper resolution regresses, these tests fail loudly.
/// </summary>
public class StorageCacheServiceStrictChainTests
{
    [Fact(DisplayName = "Round-trips IReadOnlyList<T> through StorageCacheService with NO reflection fallback")]
    public async Task StrictChain_IReadOnlyListOfT_RoundTrips()
    {
        var serializer = BuildStrictSerializer();
        var storage = new InMemoryFileStorage(serializer);
        var cache = new StorageCacheService(storage, serializer, new FakeTimeProvider(new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)));

        IReadOnlyList<TestEntity> input = new List<TestEntity>
        {
            new("a", "Alpha"),
            new("b", "Beta")
        };

        // 1) Set
        var setResult = await cache.Set("rides:current", input);
        setResult.ShouldNotBeNull();
        setResult.Value.Count.ShouldBe(2);

        // 2) Get from a separate call path — proves the round-trip survives serialization both ways.
        var getResult = await cache.Get<IReadOnlyList<TestEntity>>("rides:current", CancellationToken.None);
        getResult.ShouldNotBeNull();
        getResult!.Value.Count.ShouldBe(2);
        getResult.Value[1].Id.ShouldBe("b");
        getResult.Value[1].Name.ShouldBe("Beta");
    }


    [Fact(DisplayName = "GetOrCreate factory runs once, stored envelope is reused on subsequent calls")]
    public async Task StrictChain_GetOrCreate_FactoryRunsOnce()
    {
        var serializer = BuildStrictSerializer();
        var storage = new InMemoryFileStorage(serializer);
        var cache = new StorageCacheService(storage, serializer, new FakeTimeProvider(new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)));

        var factoryHits = 0;
        Task<IReadOnlyList<TestEntity>> Factory() => Task.FromResult<IReadOnlyList<TestEntity>>(
            new List<TestEntity> { new($"{++factoryHits}", $"name-{factoryHits}") }
        );

        var first = await cache.GetOrCreate("rides:current", Factory);
        var second = await cache.GetOrCreate("rides:current", Factory);

        factoryHits.ShouldBe(1);
        first!.Value.Count.ShouldBe(1);
        first.Value[0].Id.ShouldBe("1");
        second!.Value.Count.ShouldBe(1);
        second.Value[0].Id.ShouldBe("1");
    }


    static Shiny.ISerializer BuildStrictSerializer()
    {
        var s = new DefaultJsonSerializer();
        // Resolver order matters: AppSupportJsonResolver handles InternalCacheEntry,
        // TestEntityResolver handles TestEntity + IReadOnlyList<TestEntity>. No DefaultJsonTypeInfoResolver
        // is added — any unregistered type would throw, just like production.
        s.Options.TypeInfoResolverChain.Add(AppSupportJsonResolver.Instance);
        s.Options.TypeInfoResolverChain.Add(TestEntityResolver.Instance);
        return s;
    }


    // Hand-rolled resolver standing in for what MediatorContractsJsonResolverGenerator would emit
    // for a user's TestEntity contract type. Keeps the test self-contained.
    sealed class TestEntityResolver : IJsonTypeInfoResolver
    {
        public static readonly TestEntityResolver Instance = new();

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type == typeof(TestEntity))
                return JsonMetadataServices.CreateValueInfo<TestEntity>(options, new TestEntityConverter());

            if (type == typeof(IReadOnlyList<TestEntity>))
                return JsonMetadataServices.CreateValueInfo<IReadOnlyList<TestEntity>>(options, new ReadOnlyListOfTestEntityConverter());

            if (type == typeof(List<TestEntity>))
            {
                var elem = options.GetTypeInfo(typeof(TestEntity)) as JsonTypeInfo<TestEntity>;
                if (elem is null) return null;
                return JsonMetadataServices.CreateListInfo<List<TestEntity>, TestEntity>(
                    options,
                    new JsonCollectionInfoValues<List<TestEntity>>
                    {
                        ObjectCreator = static () => new List<TestEntity>(),
                        ElementInfo = elem
                    });
            }

            return null;
        }
    }


    sealed class TestEntityConverter : JsonConverter<TestEntity>
    {
        public override TestEntity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            string? id = null, name = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return new TestEntity(id!, name!);
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var prop = reader.GetString();
                reader.Read();
                if (prop == "Id") id = reader.GetString();
                else if (prop == "Name") name = reader.GetString();
                else reader.Skip();
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, TestEntity value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Id", value.Id);
            writer.WriteString("Name", value.Name);
            writer.WriteEndObject();
        }
    }


    sealed class ReadOnlyListOfTestEntityConverter : JsonConverter<IReadOnlyList<TestEntity>>
    {
        public override IReadOnlyList<TestEntity>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
            var list = new List<TestEntity>();
            var elementInfo = (JsonTypeInfo<TestEntity>)options.GetTypeInfo(typeof(TestEntity));
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return list;
                list.Add(JsonSerializer.Deserialize(ref reader, elementInfo)!);
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<TestEntity> value, JsonSerializerOptions options)
        {
            var elementInfo = (JsonTypeInfo<TestEntity>)options.GetTypeInfo(typeof(TestEntity));
            writer.WriteStartArray();
            for (var i = 0; i < value.Count; i++)
                JsonSerializer.Serialize(writer, value[i], elementInfo);
            writer.WriteEndArray();
        }
    }


    // Real AbstractFileStorageService against an in-memory dictionary — exercises the full
    // serialize-to-string → write → read → parse path so AppSupportJsonResolver is genuinely
    // load-bearing (unlike MockStorageService, which keeps raw object references).
    sealed class InMemoryFileStorage(Shiny.ISerializer serializer)
        : AbstractFileStorageService(serializer, NullLogger.Instance)
    {
        readonly Dictionary<string, string> files = new();

        protected override Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
        {
            this.files[fileName] = content;
            return Task.CompletedTask;
        }

        protected override Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
            => Task.FromResult(this.files.TryGetValue(fileName, out var content) ? content : null);

        protected override Task DeleteFile(string fileName, CancellationToken cancellationToken)
        {
            this.files.Remove(fileName);
            return Task.CompletedTask;
        }
    }


    public record TestEntity(string Id, string Name);
}
