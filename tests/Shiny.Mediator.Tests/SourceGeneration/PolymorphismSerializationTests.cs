using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Mediator.Tests.SourceGeneration;


/// <summary>
/// Runtime round-trips of the discriminated models generated from polymorphism-union.json
/// (ASP.NET Core anyOf + mapping) and polymorphism-inheritance.json (NSwag allOf inheritance),
/// each compiled twice - reflection (PolyUnion/PolyInherit) and generated converters wired through
/// the generated resolver only, no reflection fallback (PolyUnionGenerated/PolyInheritGenerated).
/// </summary>
public class PolymorphismSerializationTests
{
    const string DrawingJson = """
    {
      "name": "art",
      "primary": { "radius": 2.5, "$type": "circle", "color": "red" },
      "shapes": [
        { "$type": "square", "side": 4, "color": "blue" },
        { "color": "green", "radius": 1, "$type": "circle" }
      ]
    }
    """;

    const string ZooJson = """
    {
      "animals": [
        { "kind": "dog", "name": "Rex", "breed": "Lab" },
        { "name": "Bit", "ageInWeeks": 8, "breed": "Pug", "kind": "puppy" },
        { "kind": "Cat", "name": "Tom", "lives": 9 }
      ]
    }
    """;


    static JsonSerializerOptions GeneratedOnly<T>()
    {
        var ns = typeof(T).Namespace!;
        var resolverType = typeof(T).Assembly.GetType($"{ns}.{ns}JsonResolver");
        resolverType.ShouldNotBeNull();

        var resolver = (IJsonTypeInfoResolver)resolverType
            .GetField("Instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

        return new JsonSerializerOptions { TypeInfoResolver = resolver };
    }


    [Fact]
    public void Union_Reflection_RoundTrip()
    {
        var drawing = JsonSerializer.Deserialize<PolyUnion.Drawing>(DrawingJson)!;

        var primary = drawing.Primary.ShouldBeOfType<PolyUnion.ShapeCircle>();
        primary.Radius.ShouldBe(2.5);
        primary.Color.ShouldBe("red");
        primary.Type.ShouldBe("circle");

        drawing.Shapes.Count.ShouldBe(2);
        drawing.Shapes[0].ShouldBeOfType<PolyUnion.ShapeSquare>().Side.ShouldBe(4);
        drawing.Shapes[1].ShouldBeOfType<PolyUnion.ShapeCircle>().Color.ShouldBe("green");

        AssertRoundTrip(drawing, JsonSerializerOptions.Default, "$type");
    }


    [Fact]
    public void Union_GeneratedConverters_RoundTrip()
    {
        var options = GeneratedOnly<PolyUnionGenerated.Drawing>();
        var drawing = JsonSerializer.Deserialize<PolyUnionGenerated.Drawing>(DrawingJson, options)!;

        var primary = drawing.Primary.ShouldBeOfType<PolyUnionGenerated.ShapeCircle>();
        primary.Radius.ShouldBe(2.5);
        primary.Color.ShouldBe("red");

        drawing.Shapes.Count.ShouldBe(2);
        drawing.Shapes[0].ShouldBeOfType<PolyUnionGenerated.ShapeSquare>().Side.ShouldBe(4);
        drawing.Shapes[1].ShouldBeOfType<PolyUnionGenerated.ShapeCircle>().Color.ShouldBe("green");

        AssertRoundTrip(drawing, options, "$type");
    }


    [Fact]
    public void Inheritance_Reflection_RoundTrip()
    {
        var zoo = JsonSerializer.Deserialize<PolyInherit.Zoo>(ZooJson)!;
        AssertZoo(zoo.Animals!.Cast<object>().ToList(), typeof(PolyInherit.Dog), typeof(PolyInherit.Puppy), typeof(PolyInherit.Cat));

        var puppy = (PolyInherit.Puppy)zoo.Animals![1];
        puppy.Name.ShouldBe("Bit");
        puppy.Breed.ShouldBe("Pug");
        puppy.AgeInWeeks.ShouldBe(8);

        AssertRoundTrip(zoo, JsonSerializerOptions.Default, "kind");
    }


    [Fact]
    public void Inheritance_GeneratedConverters_RoundTrip()
    {
        var options = GeneratedOnly<PolyInheritGenerated.Zoo>();
        var zoo = JsonSerializer.Deserialize<PolyInheritGenerated.Zoo>(ZooJson, options)!;
        AssertZoo(zoo.Animals!.Cast<object>().ToList(), typeof(PolyInheritGenerated.Dog), typeof(PolyInheritGenerated.Puppy), typeof(PolyInheritGenerated.Cat));

        // inherited properties come through the generated per-type converters
        var puppy = (PolyInheritGenerated.Puppy)zoo.Animals![1];
        puppy.Name.ShouldBe("Bit");
        puppy.Breed.ShouldBe("Pug");
        puppy.AgeInWeeks.ShouldBe(8);

        AssertRoundTrip(zoo, options, "kind");
    }


    [Fact]
    public void New_Variant_Writes_Its_Discriminator()
    {
        PolyUnion.Shape shape = new PolyUnion.ShapeSquare { Side = 3 };
        var json = JsonSerializer.Serialize(shape);
        json.ShouldStartWith("{\"$type\":\"square\"");

        PolyInheritGenerated.Animal animal = new PolyInheritGenerated.Puppy { Name = "Bit", AgeInWeeks = 2 };
        json = JsonSerializer.Serialize(animal, GeneratedOnly<PolyInheritGenerated.Zoo>());
        json.ShouldStartWith("{\"kind\":\"puppy\"");
    }


    [Theory]
    [InlineData("""{ "kind": "lizard", "name": "x" }""", "Unknown 'kind' discriminator 'lizard'")]
    [InlineData("""{ "name": "x" }""", "Missing 'kind' discriminator")]
    public void Bad_Discriminator_Throws(string json, string message)
    {
        var ex = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<PolyInherit.Animal>(json));
        ex.Message.ShouldContain(message);
    }


    static void AssertZoo(List<object> animals, params Type[] expected)
    {
        animals.Count.ShouldBe(expected.Length);
        for (var i = 0; i < expected.Length; i++)
            animals[i].GetType().ShouldBe(expected[i]);
    }


    static void AssertRoundTrip<T>(T value, JsonSerializerOptions options, string discriminator)
    {
        var json = JsonSerializer.Serialize(value, options);
        using var doc = JsonDocument.Parse(json);

        // every polymorphic object leads with its discriminator so strict readers (STJ without
        // AllowOutOfOrderMetadataProperties) on the other side accept it
        foreach (var obj in FindObjectsWith(doc.RootElement, discriminator))
            obj.EnumerateObject().First().Name.ShouldBe(discriminator);

        var again = JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(json, options), options);
        again.ShouldBe(json);
    }


    static IEnumerable<JsonElement> FindObjectsWith(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(property, out _))
                yield return element;

            foreach (var child in element.EnumerateObject())
                foreach (var found in FindObjectsWith(child.Value, property))
                    yield return found;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                foreach (var found in FindObjectsWith(item, property))
                    yield return found;
        }
    }
}
