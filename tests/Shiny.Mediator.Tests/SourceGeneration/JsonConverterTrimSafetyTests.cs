using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


/// <summary>
/// Generated JSON converters stay trim- and AOT-safe: a member the converter does not read or write
/// itself goes through the options' JsonTypeInfo, never through an overload of JsonSerializer marked
/// [RequiresUnreferencedCode] / [RequiresDynamicCode] (IL2026 / IL3050 in a trimmed or AOT app).
/// The generated code is compiled and every call is bound, so the check is the one the trim and AOT
/// analyzers make, without publishing anything.
/// </summary>
public class JsonConverterTrimSafetyTests
{
    [Fact]
    public void Detector_Sees_The_Options_Overloads()
    {
        // guards the check below: without the framework's attributes in the references it would find nothing
        var tree = CSharpSyntaxTree.ParseText("""
            using System.Text.Json;

            static class Control
            {
                static void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
                    => JsonSerializer.Serialize(writer, new System.DateOnly(2026, 11, 12), options);

                static int Read(ref Utf8JsonReader reader, JsonSerializerOptions options)
                    => JsonSerializer.Deserialize<int>(ref reader, options);
            }
            """);
        var compilation = Compile([tree]);

        Errors(compilation).ShouldBeEmpty();
        CallsRequiringUnreferencedOrDynamicCode(compilation, [tree]).Length.ShouldBe(2);
    }


    [Fact]
    public void SourceGenerateJsonConverter_Types_Read_And_Write_Members_Through_JsonTypeInfo()
    {
        var source = CSharpSyntaxTree.ParseText("""
            using System;
            using System.Collections.Generic;

            namespace TrimApp;

            public class Child
            {
                public string Name { get; set; }
            }

            [SourceGenerateJsonConverter]
            public partial class Parent
            {
                public string Name { get; set; }
                public Child Child { get; set; }
                public List<Guid> Ids { get; set; }
                public DateOnly Day { get; set; }
                public DateOnly? Until { get; set; }
                public Dictionary<string, int> Counts { get; set; }
            }

            [SourceGenerateJsonConverter]
            public partial record Line(Guid Id, List<string> Tags, TimeSpan? Duration);

            [SourceGenerateJsonConverter]
            public partial struct Point
            {
                public int X { get; set; }
                public int[] Path { get; set; }
            }
            """);

        CSharpGeneratorDriver.Create(new JsonConverterSourceGenerator())
            .RunGeneratorsAndUpdateCompilation(Compile([source]), out var output, out _);
        var generated = output.SyntaxTrees.Where(x => x != source).ToArray();

        Errors(output).ShouldBeEmpty();
        generated.Count(x => x.FilePath.EndsWith("JsonConverter.g.cs")).ShouldBe(3);
        CallsRequiringUnreferencedOrDynamicCode(output, generated).ShouldBeEmpty();
    }


    [Fact]
    public void OpenApi_Converters_Read_And_Write_Members_Through_JsonTypeInfo()
    {
        const string path = "./SourceGeneration/trim-safety.json";
        var dummy = CSharpSyntaxTree.ParseText("namespace Dummy { public class C { } }");
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "TrimApi",
            ["build_metadata.AdditionalFiles.GenerateJsonConverters"] = "true",
            ["build_property.RootNamespace"] = "UnitTests",
            ["build_property.AssemblyName"] = "UnitTests"
        });

        CSharpGeneratorDriver.Create(
                generators: [new OpenApiHttpClientSourceGenerator().AsSourceGenerator()],
                additionalTexts: [new MockAdditionalText(path, File.ReadAllText(path))],
                parseOptions: (CSharpParseOptions)dummy.Options,
                optionsProvider: options)
            .RunGeneratorsAndUpdateCompilation(Compile([dummy]), out var output, out _);
        var generated = output.SyntaxTrees.Where(x => x != dummy).ToArray();

        Errors(output).ShouldBeEmpty();
        generated.ShouldContain(x => x.FilePath.EndsWith("OrderJsonConverter.g.cs"));
        CallsRequiringUnreferencedOrDynamicCode(output, generated).ShouldBeEmpty();
    }


    // everything the test host runs on: the framework (with its trim/AOT attributes) and Shiny.Mediator
    static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
        .ToArray();

    static CSharpCompilation Compile(SyntaxTree[] trees) => CSharpCompilation.Create(
        "TrimSafety",
        trees,
        References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    static string[] Errors(Compilation compilation) => compilation
        .GetDiagnostics()
        .Where(x => x.Severity == DiagnosticSeverity.Error)
        .Select(x => x.ToString())
        .ToArray();

    static string[] CallsRequiringUnreferencedOrDynamicCode(Compilation compilation, IEnumerable<SyntaxTree> trees) => trees
        .SelectMany(tree =>
        {
            var model = compilation.GetSemanticModel(tree);
            return tree
                .GetRoot()
                .DescendantNodes()
                .Where(x => x is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)
                .Select(x => model.GetSymbolInfo(x).Symbol)
                .OfType<IMethodSymbol>()
                .Where(RequiresUnreferencedOrDynamicCode)
                .Select(x => $"{Path.GetFileName(tree.FilePath)}: {x.ToDisplayString()}");
        })
        .Distinct()
        .ToArray();

    // the analyzers also apply an attribute on the type to its constructors and static members
    static bool RequiresUnreferencedOrDynamicCode(IMethodSymbol method) => method.OriginalDefinition
        .GetAttributes()
        .Concat(method.ContainingType.GetAttributes())
        .Any(x => x.AttributeClass?.ToDisplayString() is
            "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute" or
            "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute");
}
