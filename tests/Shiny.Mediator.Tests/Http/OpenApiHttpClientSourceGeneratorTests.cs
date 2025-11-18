using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Shiny.Mediator.SourceGenerators;
using System.Text;
using Shiny.Mediator.Tests.SourceGeneration;

namespace Shiny.Mediator.Tests.Http;


public class OpenApiHttpClientSourceGeneratorTests
{
    [Fact]
    public Task ThemeParksApiGeneration()
    {
        var content = File.ReadAllText("./Http/themeparksapi.yml");
        var additionalFiles = new AdditionalText[] { new MockAdditionalText("./Http/themeparksapi.yml", content) };

        var buildProps = new Dictionary<string, string>
        {
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "TestApi",
            ["build_property.RootNamespace"] = "UnitTests",
            ["build_property.AssemblyName"] = "UnitTests"
        };

        var result = RunGenerator(additionalFiles, buildProps);
        return Verify(result);
    }
    
    [Fact]
    public Task Response_With_Json_Schema_Uses_Schema_Type_Not_HttpResponseMessage()
    {
        // Arrange: minimal OpenAPI with 200 -> application/json -> $ref Item
        var openApi = """
        openapi: 3.0.1
        info:
          title: Test API
          version: '1.0'
        paths:
          /items:
            get:
              operationId: getItems
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Item'
        components:
          schemas:
            Item:
              type: object
              properties:
                id:
                  type: string
                name:
                  type: string
        """;

        var additionalFiles = new AdditionalText[] { new MockAdditionalText("items.yaml", openApi) };

        var buildProps = new Dictionary<string, string>
        {
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "TestApi",
            ["build_property.RootNamespace"] = "UnitTests",
            ["build_property.AssemblyName"] = "UnitTests"
        };

        var result = RunGenerator(additionalFiles, buildProps);
        return Verify(result);
    }

    [Fact]
    public Task Response_With_204_NoContent_Falls_Back_To_HttpResponseMessage()
    {
        // Arrange: minimal OpenAPI with only 204 no content
        var openApi = """
        openapi: 3.0.1
        info:
          title: Test API
          version: '1.0'
        paths:
          /ping:
            post:
              operationId: ping
              responses:
                '204':
                  description: No Content
        """;

        var additionalFiles = new AdditionalText[] { new MockAdditionalText("ping.yaml", openApi) };

        var buildProps = new Dictionary<string, string>
        {
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "TestApi",
            ["build_property.RootNamespace"] = "UnitTests",
            ["build_property.AssemblyName"] = "UnitTests"
        };

        var result = RunGenerator(additionalFiles, buildProps);
        return Verify(result);
    }

    static GeneratorRunResult RunGenerator(AdditionalText[] additionalFiles, Dictionary<string, string> buildProps)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Dummy { public class C { } }
            """);

        var compilation = CSharpCompilation.Create(
            assemblyName: "UnitTests.Dynamic",
            syntaxTrees: [syntaxTree],
            references: [
                MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ICommand).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MediatorHttpGroupAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new OpenApiHttpClientSourceGenerator();
        var optionsProvider = new MockAnalyzerConfigOptionsProvider(buildProps);

        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalFiles,
            parseOptions: syntaxTree.Options as CSharpParseOptions,
            optionsProvider: optionsProvider);

        var runDriver = driver.RunGenerators(compilation);
        return runDriver.GetRunResult().Results[0];
    }
}

internal class MockAdditionalText(string path, string content) : AdditionalText
{
    public override string Path { get; } = path;
    public override SourceText? GetText(CancellationToken cancellationToken = default) => SourceText.From(content, Encoding.UTF8);
}
