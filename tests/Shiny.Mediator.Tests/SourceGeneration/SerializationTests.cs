using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class SerializationTests
{
    [Fact]
    public Task Reader_NonJsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApi.EntityLiveDataResponse>(json);
        return Verify(obj);
    }

    [Fact]
    public Task Reader_Generated_JsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApiGenerated.EntityLiveDataResponse>(json);
        return Verify(obj);
    }

    [Fact]
    public Task Writer_NonJsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApi.EntityLiveDataResponse>(json);
        
        var serializedJson = JsonSerializer.Serialize(obj);
        return Verify(serializedJson);
    }
    
    
    [Fact]
    public Task Writer_Generated_JsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApiGenerated.EntityLiveDataResponse>(json);
        
        var serializedJson = JsonSerializer.Serialize(obj);
        return Verify(serializedJson);
    }
    
    
    [Fact]
    public Task GenerateJsonConverter_ForPartialClass_ShouldGenerateCorrectCode()
    {
        // Arrange
        var source = """
            using System;
            
            [SourceGenerateJsonConverter]
            public partial class TestModel
            {
                public string Name { get; set; } = string.Empty;
                public int Age { get; set; }
                public string? Email { get; set; }
                public DateTime? BirthDate { get; set; }
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        return Verify(result);
    }

    [Fact]
    public Task GenerateJsonConverter_ForStruct_ShouldGenerateCorrectCode()
    {
        // Arrange
        var source = """
            using System;
            
            [SourceGenerateJsonConverter]
            public struct Point
            {
                public double X { get; set; }
                public double Y { get; set; }
                public string? Label { get; set; }
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        return Verify(result);
    }

    [Fact]
    public Task GenerateJsonConverter_ForRecord_ShouldGenerateCorrectCode()
    {
        // Arrange
        var source = """
            using System;

            [SourceGenerateJsonConverter]
            public partial record VehicleResult(int Id, string Manufacturer, string Model)
            {
                public string Name => $"{Manufacturer} {Model}";
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        return Verify(result);
    }

    [Fact]
    public Task GenerateJsonConverter_ForRecordWithInitProperties_ShouldUseObjectInitializer()
    {
        // Regression: a record declared with a body and init-only properties has no positional
        // primary constructor, only a parameterless one. The generator previously emitted
        // `new AnchorPointDto(x, y)` (CS1729 — no such constructor). It must now construct via an
        // object initializer: `new AnchorPointDto() { X = x, Y = y }`. [JsonPropertyName] is also
        // honored, so the wire names are lowercase "x"/"y".
        var source = """
            using System.Text.Json.Serialization;

            [SourceGenerateJsonConverter]
            public partial record AnchorPointDto
            {
                [JsonPropertyName("x")]
                public float X { get; init; }

                [JsonPropertyName("y")]
                public float Y { get; init; }
            }
            """;

        var result = TestHelper.RunSourceGenerator(source);
        return Verify(result);
    }

    [Fact]
    public void RecordWithInitProperties_GeneratedConverter_RoundTrips()
    {
        // Proves the generated converter both compiles (the assembly wouldn't build otherwise) and
        // round-trips through the lowercase [JsonPropertyName] wire names.
        var json = """{"x":1.5,"y":2.5}""";
        var point = JsonSerializer.Deserialize<TestModels.AnchorPointDto>(json);

        point.ShouldNotBeNull();
        point.X.ShouldBe(1.5f);
        point.Y.ShouldBe(2.5f);

        JsonSerializer.Serialize(point).ShouldBe(json);
    }

    [Fact]
    public void RecordWithInitProperties_SystemTextJson_RoundTrips()
    {
        // Baseline: System.Text.Json handles the init-only record + [JsonPropertyName] on its own
        // (no generated converter on AnchorPointStj). The generated converter must match this.
        var json = """{"x":1.5,"y":2.5}""";
        var point = JsonSerializer.Deserialize<TestModels.AnchorPointStj>(json);

        point.ShouldNotBeNull();
        point.X.ShouldBe(1.5f);
        point.Y.ShouldBe(2.5f);

        JsonSerializer.Serialize(point).ShouldBe(json);
    }

    [Fact]
    public Task GenerateJsonConverter_ForNullableEnum_ShouldEmitEnumParseAndAvoidNullableConverter()
    {
        // Regression: previously the generator detected only typeSymbol.TypeKind == Enum.
        // For Nullable<TEnum> the symbol is Nullable<T> (TypeKind.Struct), so the generator
        // fell through to JsonSerializer.Deserialize<TEnum?>(...) and JsonSerializer.Serialize(...).
        // At runtime under NativeAOT, STJ resolves NullableConverter<TEnum> via reflection and
        // throws "missing native code or metadata". The fix unwraps Nullable<TEnum> and emits
        // inline Enum.Parse<TEnum>(reader.GetString()!) cast to TEnum?.
        var source = """
            using System;

            public enum Status { Open, Closed }

            [SourceGenerateJsonConverter]
            public partial class Entity
            {
                public Status Required { get; set; }
                public Status? Optional { get; set; }
            }
            """;

        var result = TestHelper.RunSourceGenerator(source);
        return Verify(result);
    }

    [Fact]
    public void NullableEnumRoundtrip_PopulatedValue()
    {
        var json = """{"Required":"Closed","Optional":"Open"}""";
        var entity = JsonSerializer.Deserialize<TestModels.Entity>(json);

        entity.ShouldNotBeNull();
        entity.Required.ShouldBe(TestModels.Status.Closed);
        entity.Optional.ShouldBe(TestModels.Status.Open);

        var roundtrip = JsonSerializer.Serialize(entity);
        roundtrip.ShouldBe(json);
    }

    [Fact]
    public void NullableEnumRoundtrip_NullValue()
    {
        var json = """{"Required":"Open","Optional":null}""";
        var entity = JsonSerializer.Deserialize<TestModels.Entity>(json);

        entity.ShouldNotBeNull();
        entity.Required.ShouldBe(TestModels.Status.Open);
        entity.Optional.ShouldBeNull();

        var roundtrip = JsonSerializer.Serialize(entity);
        roundtrip.ShouldBe(json);
    }

    [Fact]
    public void GenerateJsonConverter_ForNonPartialClass_ShouldReportDiagnostic()
    {
        // Arrange
        var source = """
            using System;
            
            [SourceGenerateJsonConverter]
            public class NonPartialClass
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        result.Diagnostics.ShouldNotBeEmpty();
        result.Diagnostics.ShouldContain(d => d.Id == "SJSG001");
        // result.Diagnostics.ShouldContain(d => d.GetMessage().Contains("must be declared as partial"));
    }

    [Fact]
    public void GenerateJsonConverter_WithNoProperties_ShouldGenerateEmptyConverter()
    {
        // Arrange
        var source = """
            [SourceGenerateJsonConverter]
            public partial class EmptyClass
            {
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedSources.ShouldNotBeEmpty();
    }

    [Fact(Skip = "TODO: Fix this test")]
    public void GenerateJsonConverter_WithPrivateProperties_ShouldIgnorePrivateProperties()
    {
        // Arrange
        var source = """
            [SourceGenerateJsonConverter]
            public partial class TestClass
            {
                public string PublicProp { get; set; } = string.Empty;
                private string PrivateProp { get; set; } = string.Empty;
                internal string InternalProp { get; set; } = string.Empty;
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedSources.ShouldNotBeEmpty();
        
        var converterSource = result.GeneratedSources.FirstOrDefault(s => s.HintName.Contains("JsonConverter"));
        // converterSource.ShouldNotBeNull();
        // converterSource.SourceText.ToString().ShouldContain("PublicProp");
        // converterSource.SourceText.ToString().ShouldNotContain("PrivateProp");
        // converterSource.SourceText.ToString().ShouldNotContain("InternalProp");
    }
}
    
    
public static class TestHelper
{
    public static GeneratorRunResult RunSourceGenerator(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new JsonConverterSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);

        return driver.GetRunResult().Results[0];
    }

    static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonConverterAttribute).Assembly.Location),
            // System.Runtime / netstandard are required for attribute applications (e.g.
            // [JsonPropertyName("x")]) to bind their constructor arguments — without them the
            // attribute resolves as an error type and the generator silently falls back to the C#
            // property name instead of the JSON name.
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

