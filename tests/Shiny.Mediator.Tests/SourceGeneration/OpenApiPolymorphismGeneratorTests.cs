using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;

public class OpenApiPolymorphismGeneratorTests
{
    static JsonNode Schema() => JsonNode.Parse(File.ReadAllText("SourceGeneration/polymorphism.json"))!;

    [Fact]
    public Task Mapped_Variants_Emit_Hierarchy_Converters_And_Resolver()
    {
        var (result, compilation) = Generate(Schema());
        AssertCompiles(result, compilation);
        return Verify(result);
    }

    [Fact]
    public void AnyOf_Exporter_Shape_Has_The_Same_Discriminated_Model_As_OneOf()
    {
        var schema = Schema();
        foreach (var name in new[] { "OrderItemSource", "VoucherOriginData" })
        {
            var root = schema["components"]!["schemas"]![name]!.AsObject();
            root["anyOf"] = root["oneOf"]!.DeepClone();
            root.Remove("oneOf");
        }
        var (actual, compilation) = Generate(schema);
        var (expected, _) = Generate(Schema());
        AssertCompiles(actual, compilation);
        actual.GeneratedSources.Select(x => x.SourceText.ToString()).ShouldBe(expected.GeneratedSources.Select(x => x.SourceText.ToString()));
    }

    [Fact]
    public void Default_Off_Preserves_Existing_Generation()
    {
        var (result, _) = Generate(Schema(), enabled: false);
        result.GeneratedSources.ShouldNotContain(x => x.SourceText.ToString().Contains("abstract partial class"));
    }

    [Fact]
    public void Internal_Models_And_Their_Converters_Compile()
    {
        var (result, compilation) = Generate(Schema(), useInternal: true);
        AssertCompiles(result, compilation);
    }

    [Fact]
    public void Discriminator_Names_And_Values_Are_CSharp_Escaped()
    {
        var schema = Schema();
        var root = schema["components"]!["schemas"]!["VoucherOriginData"]!;
        root["required"] = new JsonArray("origin\"Kind");
        root["discriminator"]!["propertyName"] = "origin\"Kind";
        var mapping = root["discriminator"]!["mapping"]!.AsObject();
        var target = mapping["legacyImport"]!.DeepClone();
        mapping.Remove("legacyImport");
        mapping["legacy\"Import\\source"] = target;
        var (result, compilation) = Generate(schema);
        AssertCompiles(result, compilation);
    }

    [Fact]
    public void Compatible_Constant_Tag_Is_Supported()
    {
        var schema = Schema();
        schema["components"]!["schemas"]!["CatalogItem"]!["properties"]!["kind"]!["const"] = "catalogItem";
        var (result, compilation) = Generate(schema);
        AssertCompiles(result, compilation);
    }

    [Theory]
    [InlineData("missing-mapping")]
    [InlineData("incomplete-mapping")]
    [InlineData("duplicate-mapping")]
    [InlineData("inline-variant")]
    [InlineData("inline-base")]
    [InlineData("external-variant")]
    [InlineData("nested-variant")]
    [InlineData("allof-variant")]
    [InlineData("redeclared-common-property")]
    [InlineData("wrong-tag")]
    [InlineData("wrong-const-tag")]
    public void Unsupported_Schema_Is_A_Clear_Generator_Error(string scenario)
    {
        var schema = Schema();
        var schemas = schema["components"]!["schemas"]!.AsObject();
        var source = schemas["OrderItemSource"]!;
        var mapping = source["discriminator"]!["mapping"]!.AsObject();
        switch (scenario)
        {
            case "missing-mapping": source["discriminator"]!.AsObject().Remove("mapping"); break;
            case "incomplete-mapping": mapping.Remove("customSupply"); break;
            case "duplicate-mapping": mapping["alias"] = "#/components/schemas/CatalogItem"; break;
            case "inline-variant": source["oneOf"]![0] = new JsonObject { ["type"] = "object" }; break;
            case "inline-base": schemas["NewOrderItem"]!["properties"]!["source"] = source.DeepClone(); break;
            case "external-variant": mapping["catalogItem"] = "other.json#/components/schemas/CatalogItem"; break;
            case "nested-variant": schemas["CatalogItem"]!["discriminator"] = source["discriminator"]!.DeepClone(); break;
            case "allof-variant": schemas["CatalogItem"]!["allOf"] = new JsonArray(new JsonObject { ["type"] = "object" }); break;
            case "redeclared-common-property": schemas["CatalogItem"]!["properties"]!["correlationId"] = new JsonObject { ["type"] = "integer" }; break;
            case "wrong-tag": schemas["CatalogItem"]!["properties"]!["kind"]!["enum"] = new JsonArray("different"); break;
            case "wrong-const-tag": schemas["CatalogItem"]!["properties"]!["kind"]!["const"] = "different"; break;
        }
        var (result, _) = Generate(schema);
        result.Diagnostics.ShouldContain(x => x.Id == "SHINYMED005" && x.Severity == DiagnosticSeverity.Error);
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void Mapped_Hierarchy_Requires_Generated_Converters()
    {
        var (result, _) = Generate(Schema(), converters: false);
        result.Diagnostics.ShouldContain(x => x.Id == "SHINYMED005" && x.GetMessage().Contains("GenerateJsonConverters"));
    }

    static void AssertCompiles(GeneratorRunResult result, Compilation compilation)
    {
        result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        using var output = new MemoryStream();
        var emitted = compilation.Emit(output);
        emitted.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        emitted.Diagnostics.ShouldNotContain(x => x.Id == "CS8669");
    }

    static (GeneratorRunResult Result, Compilation Compilation) Generate(JsonNode schema, bool enabled = true, bool converters = true, bool useInternal = false)
    {
        var syntax = CSharpSyntaxTree.ParseText("namespace GeneratedFixture { public class Marker {} }");
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(file => MetadataReference.CreateFromFile(file)).ToList();
        references.Add(MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, typeof(Json).Assembly.GetName().Name + ".dll")));
        var compilation = CSharpCompilation.Create("Polymorphism.Generated", [syntax], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var options = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "PolymorphicApi",
            ["build_property.ShinyMediatorOpenApiPolymorphism"] = enabled.ToString(),
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "PolymorphicApi",
            ["build_metadata.AdditionalFiles.GenerateModelsOnly"] = "true",
            ["build_metadata.AdditionalFiles.UseInternalClasses"] = useInternal.ToString(),
            ["build_metadata.AdditionalFiles.GenerateJsonConverters"] = converters.ToString()
        };
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new OpenApiHttpClientSourceGenerator().AsSourceGenerator()],
            additionalTexts: [new MockAdditionalText("polymorphism.json", schema.ToJsonString())],
            parseOptions: (CSharpParseOptions)syntax.Options,
            optionsProvider: new MockAnalyzerConfigOptionsProvider(options));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out _);
        return (driver.GetRunResult().Results.Single(), generated);
    }
}
