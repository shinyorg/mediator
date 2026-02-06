using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Shiny.Mediator.SourceGenerators;
using System.Text;
using Microsoft.Extensions.Configuration;
using ThemeParksApi;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class OpenApiHttpClientSourceGeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public async Task e2e()
    {
        var services = new ServiceCollection();
        
        services.AddXUnitLogging(output);
        services.AddConfiguration(x =>
        {
            x.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Mediator:Http:*",  "https://api.themeparks.wiki/v1/"}
            });
        });
        services.AddShinyMediator(x => x.AddGeneratedOpenApiClient());

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Request(new GetEntityLiveDataHttpRequest
        {
            EntityID = "66f5d97a-a530-40bf-a712-a6317c96b06d"
        });
    }
    
    
    [Theory]
    [InlineData("./SourceGeneration/themeparksapi.yml")]
    [InlineData("./SourceGeneration/fleet.json")]
    [InlineData("./SourceGeneration/test.json")]
    public Task TestApis_Generation(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var additionalFiles = new AdditionalText[] { new MockAdditionalText(filePath, content) };

        var buildProps = new Dictionary<string, string>
        {
            ["build_property.ShinyMediatorOpenApiRegistrationNamespace"] = "ThisShouldBeTheNamespace",
            ["build_property.ShinyMediatorOpenApiRegistrationClassName"] = "Hello",
            ["build_property.ShinyMediatorOpenApiRegistrationMethodName"] = "World",
            ["build_property.ShinyMediatorOpenApiRegistrationUseInternalClass"] = "TrUE",
            ["build_property.RootNamespace"] = "UnitTests",
            ["build_property.AssemblyName"] = "UnitTests",
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "TestApi"
        };

        var result = RunGenerator(additionalFiles, buildProps);
        return Verify(result).UseParameters(filePath);
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

    [Fact]
    public Task PathParameters_Should_Not_Appear_In_Generated_Handler_Names()
    {
        // This test ensures that when no operationId is specified, path parameters like {id}
        // are excluded from the generated handler name. Without the fix, this would generate
        // "GetAds{id}HttpRequestHandler" instead of "GetAdsHttpRequestHandler".
        var openApi = """
        openapi: 3.0.1
        info:
          title: Test API
          version: '1.0'
        paths:
          /ads/{id}:
            get:
              parameters:
                - name: id
                  in: path
                  required: true
                  schema:
                    type: string
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        properties:
                          id:
                            type: string
                          title:
                            type: string
          /users/{userId}/posts/{postId}:
            get:
              parameters:
                - name: userId
                  in: path
                  required: true
                  schema:
                    type: string
                - name: postId
                  in: path
                  required: true
                  schema:
                    type: string
              responses:
                '200':
                  description: OK
          /items/{itemId}/details:
            delete:
              parameters:
                - name: itemId
                  in: path
                  required: true
                  schema:
                    type: integer
              responses:
                '204':
                  description: No Content
        """;

        var additionalFiles = new AdditionalText[] { new MockAdditionalText("pathparams.yaml", openApi) };

        var buildProps = new Dictionary<string, string>
        {
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "TestApi",
            ["build_property.RootNamespace"] = "UnitTests",
            ["build_property.AssemblyName"] = "UnitTests"
        };

        var result = RunGenerator(additionalFiles, buildProps);

        // Verify that the generated source does NOT contain path parameters in handler names
        var generatedSources = result.GeneratedSources
            .Select(s => s.SourceText.ToString())
            .ToList();

        // Handler names should NOT contain curly braces
        foreach (var source in generatedSources)
        {
            Assert.DoesNotContain("{id}", source);
            Assert.DoesNotContain("{userId}", source);
            Assert.DoesNotContain("{postId}", source);
            Assert.DoesNotContain("{itemId}", source);
        }

        return Verify(result);
    }

    [Fact]
    public Task Hyphenated_Path_Segments_Should_Generate_Valid_CSharp_Identifiers()
    {
        // This test ensures that path segments with hyphens (like "entra-login") are
        // converted to valid C# identifiers (like "EntraLogin").
        // Without the fix, "/account/entra-login" would generate "PostAccountEntra-loginHttpRequest"
        // which is an invalid C# class name.
        var openApi = """
        openapi: 3.0.1
        info:
          title: Test API
          version: '1.0'
        paths:
          /account/entra-login:
            post:
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        properties:
                          token:
                            type: string
          /api/user-management/role-assignments:
            get:
              responses:
                '200':
                  description: OK
          /v1/health-check/ping-pong:
            get:
              responses:
                '200':
                  description: OK
        """;

        var additionalFiles = new AdditionalText[] { new MockAdditionalText("hyphens.yaml", openApi) };

        var buildProps = new Dictionary<string, string>
        {
            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "MediatorHttp",
            ["build_metadata.AdditionalFiles.Namespace"] = "TestApi",
            ["build_property.RootNamespace"] = "UnitTests",
            ["build_property.AssemblyName"] = "UnitTests"
        };

        var result = RunGenerator(additionalFiles, buildProps);

        // Verify that the generated source does NOT contain hyphens in handler/contract names (class definitions)
        var generatedSources = result.GeneratedSources
            .Select(s => s.SourceText.ToString())
            .ToList();

        // Handler/contract class names should NOT contain hyphens
        // Check class declarations, not string literals
        foreach (var source in generatedSources)
        {
            // Check that hyphenated names are NOT present as class names
            Assert.DoesNotContain("class PostAccountEntra-login", source);
            Assert.DoesNotContain("class GetApiUser-management", source);
            Assert.DoesNotContain("class GetV1Health-check", source);
        }

        // Verify that the correctly pascalized names ARE present
        var allSource = string.Join("\n", generatedSources);
        Assert.Contains("PostAccountEntraLogin", allSource);
        Assert.Contains("UserManagementRoleAssignments", allSource);
        Assert.Contains("HealthCheckPingPong", allSource);

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