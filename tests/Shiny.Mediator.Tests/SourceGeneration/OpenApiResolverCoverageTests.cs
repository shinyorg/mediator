using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Mediator.Tests.SourceGeneration;


/// <summary>
/// The generated OpenAPI resolver is the only metadata source of an app without reflection (Native AOT,
/// JsonSerializerIsReflectionEnabledByDefault=false). Every type the generated converters and handlers ask
/// the options for - members, bodies and responses - has to come from it, or the call throws
/// NotSupportedException at run time.
/// </summary>
public class OpenApiResolverCoverageTests
{
    static readonly Guid Id = Guid.Parse("0193a4b2-7c1e-7d3a-9f00-0000000000a1");
    static readonly Guid Tag = Guid.Parse("0193a4b2-7c1e-7d3a-9f00-0000000000b2");


    [Fact]
    public void Members_Roundtrip_Through_The_Generated_Resolver_Alone()
    {
        var options = Options();
        var order = new ResolverCoverageApi.Order
        {
            Id = Id,
            Day = new DateOnly(2026, 11, 12),
            ClosedOn = null,
            Tags = [Tag],
            Lines = [new ResolverCoverageApi.OrderLine { Name = "Beer", Gross = 4.90 }],
            Payment = new ResolverCoverageApi.Payment { At = new DateTimeOffset(2026, 11, 12, 18, 30, 0, TimeSpan.FromHours(1)) },
            Status = ResolverCoverageApi.OrderStatus.closed,
            Counts = new() { ["Beer"] = 2 },
            TagsByLine = new() { ["Beer"] = [Tag] }
        };

        var json = JsonSerializer.Serialize(order, Info<ResolverCoverageApi.Order>(options));
        json.ShouldContain("\"day\":\"2026-11-12\"");
        json.ShouldContain("\"counts\":{\"Beer\":2}");

        var read = JsonSerializer.Deserialize(json, Info<ResolverCoverageApi.Order>(options))!;
        read.Id.ShouldBe(Id);
        read.Day.ShouldBe(new DateOnly(2026, 11, 12));
        read.ClosedOn.ShouldBeNull();
        read.Tags.ShouldBe([Tag]);
        read.Lines.ShouldHaveSingleItem().Gross.ShouldBe(4.90);
        read.Payment!.At.ShouldBe(order.Payment.At);
        read.Status.ShouldBe(ResolverCoverageApi.OrderStatus.closed);
        read.Counts!["Beer"].ShouldBe(2);
        read.TagsByLine!["Beer"].ShouldBe([Tag]);
    }


    [Fact]
    public void An_AdditionalProperties_Response_Comes_From_The_Generated_Resolver()
    {
        // the createOrder response: an additionalProperties object at the top level
        var lineIds = JsonSerializer.Deserialize($$"""{"Beer":"{{Tag}}"}""", Info<Dictionary<string, Guid>>(Options()))!;
        lineIds["Beer"].ShouldBe(Tag);
    }


    [Fact]
    public void A_Converter_On_The_Options_Wins_Over_The_Built_In_Scalar_Converter()
    {
        var options = Options();
        options.Converters.Add(new DayNumberConverter());
        var order = new ResolverCoverageApi.Order
        {
            Id = Id,
            Day = new DateOnly(2026, 11, 12),
            ClosedOn = new DateOnly(2026, 11, 13),
            Lines = []
        };

        var json = JsonSerializer.Serialize(order, Info<ResolverCoverageApi.Order>(options));
        json.ShouldContain($"\"day\":{order.Day.DayNumber}");
        json.ShouldContain($"\"closedOn\":{order.ClosedOn!.Value.DayNumber}");

        var read = JsonSerializer.Deserialize(json, Info<ResolverCoverageApi.Order>(options))!;
        read.Day.ShouldBe(order.Day);
        read.ClosedOn.ShouldBe(order.ClosedOn);
    }


    // no reflection fallback: a type the resolver does not cover throws NotSupportedException
    static JsonSerializerOptions Options() => new() { TypeInfoResolver = ResolverCoverageApi.ResolverCoverageApiJsonResolver.Instance };

    sealed class DayNumberConverter : System.Text.Json.Serialization.JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateOnly.FromDayNumber(reader.GetInt32());

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.DayNumber);
    }

    static JsonTypeInfo<T> Info<T>(JsonSerializerOptions options) => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
