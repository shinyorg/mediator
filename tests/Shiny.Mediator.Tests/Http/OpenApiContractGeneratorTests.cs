using System.Runtime.CompilerServices;
using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.Http;


public record PathAndMediatorHttpItemConfig(string Path, MediatorHttpItemConfig Config);

public class OpenApiContractGeneratorTests(ITestOutputHelper output)
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.NameForParameter<PathAndMediatorHttpItemConfig>(x =>
            $"{Path.GetFileName(x.Path)}_{x.Config.Namespace}_{x.Config.ContractPostfix}_{x.Config.UseInternalClasses}_{x.Config.GenerateModelsOnly}_{x.Config.GenerateJsonConverters}"
        );
    }
    
    [Theory]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, false)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", true, false)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, true)]
    [InlineData("./Http/enums.json", null, "TestApi", false, false)]
    [InlineData("./Http/validateresult.json", null, "TestApi", false, false)]
    public Task Local_Tests(
        string path, 
        string? contractPostfix, 
        string nameSpace, 
        bool useInternalClasses,
        bool generateModelsOnly
    )
    {
        var args = new MediatorHttpItemConfig
        {
            Namespace = nameSpace,
            ContractPostfix = contractPostfix,
            UseInternalClasses = useInternalClasses,
            GenerateModelsOnly = generateModelsOnly
        };
        using var doc = File.OpenRead(path);
        var generator = new OpenApiContractGenerator(args, (msg, severity) => output.WriteLine($"[{severity}] {msg}"));
        var content = generator.Generate(doc);
        
        return Verify(content).UseParameters(new PathAndMediatorHttpItemConfig(path, args));
    }
    
    
    [Theory]
    [InlineData("https://api.themeparks.wiki/docs/v1.yaml", "ThemeParksApi")]
    public async Task RemoteTests(string uri, string nameSpace)
    {
        var http = new HttpClient();
        var stream = await http.GetStreamAsync(uri);
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = nameSpace,
                ContractPostfix = "HttpRequest"
            }, 
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(stream);
    
        await Verify(content).UseParameters(uri, nameSpace);
    }

    [Theory]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, false, true)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", true, false, true)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, true, true)]
    [InlineData("./Http/enums.json", null, "TestApi", false, false, true)]
    [InlineData("./Http/validateresult.json", null, "TestApi", false, false, true)]
    public Task JsonConverter_Generation_Tests(
        string path, 
        string? contractPostfix, 
        string nameSpace, 
        bool useInternalClasses,
        bool generateModelsOnly,
        bool generateJsonConverters
    )
    {
        var args = new MediatorHttpItemConfig
        {
            Namespace = nameSpace,
            ContractPostfix = contractPostfix,
            UseInternalClasses = useInternalClasses,
            GenerateModelsOnly = generateModelsOnly,
            GenerateJsonConverters = generateJsonConverters
        };
        using var doc = File.OpenRead(path);
        var generator = new OpenApiContractGenerator(args, (msg, severity) => output.WriteLine($"[{severity}] {msg}"));
        var content = generator.Generate(doc);
        
        return Verify(content).UseParameters(new PathAndMediatorHttpItemConfig(path, args));
    }

    [Fact]
    public void JsonConverter_Disabled_Should_Not_Generate_Converters()
    {
        using var doc = File.OpenRead("./Http/standard.json");
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = false
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(doc);
        
        // Should not contain any JSON converter classes
        Assert.DoesNotContain("JsonConverter", content);
        Assert.DoesNotContain("System.Text.Json.Serialization.JsonConverter", content);
        
        // Should not contain converter attributes on classes
        Assert.DoesNotContain("[global::System.Text.Json.Serialization.JsonConverter(typeof(", content);
    }

    [Fact]
    public void JsonConverter_Enabled_Should_Generate_Converters()
    {
        using var doc = File.OpenRead("./Http/standard.json");
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = true
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(doc);
        
        // Should contain JSON converter classes
        Assert.Contains("JsonConverter", content);
        Assert.Contains("System.Text.Json.Serialization.JsonConverter", content);
        
        // Should contain converter attributes on classes
        Assert.Contains("[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof(", content);
        
        // Should contain Read and Write methods
        Assert.Contains("public override", content);
        Assert.Contains("Read(ref global::System.Text.Json.Utf8JsonReader reader", content);
        Assert.Contains("Write(global::System.Text.Json.Utf8JsonWriter writer", content);
    }

    [Fact]
    public void JsonConverter_Should_Use_Fully_Qualified_Names()
    {
        using var doc = File.OpenRead("./Http/standard.json");
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = true
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(doc);
        
        // Verify fully qualified type names are used
        Assert.Contains("global::System.Text.Json.Serialization.JsonConverter", content);
        Assert.Contains("global::System.Text.Json.Utf8JsonReader", content);
        Assert.Contains("global::System.Text.Json.Utf8JsonWriter", content);
        Assert.Contains("global::System.Text.Json.JsonTokenType", content);
        Assert.Contains("global::System.Text.Json.JsonException", content);
        Assert.Contains("global::System.Type", content);
        Assert.Contains("global::System.Text.Json.JsonSerializerOptions", content);
    }

    [Fact]
    public void JsonConverter_Should_Handle_Internal_Classes()
    {
        using var doc = File.OpenRead("./Http/standard.json");
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = true,
                UseInternalClasses = true
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(doc);
        
        // Should generate internal converter classes when UseInternalClasses is true
        Assert.Contains("internal class", content);
        Assert.Contains("JsonConverter", content);
    }

    [Fact]
    public void JsonConverter_Should_Handle_Public_Classes()
    {
        using var doc = File.OpenRead("./Http/standard.json");
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = true,
                UseInternalClasses = false
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(doc);
        
        // Should generate public converter classes when UseInternalClasses is false
        Assert.Contains("public class", content);
        Assert.Contains("JsonConverter", content);
    }

    [Fact] 
    public void JsonConverter_Should_Track_Request_Body_Types()
    {
        // Create a simple OpenAPI spec with request body
        var openApiJson = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/test": {
              "post": {
                "operationId": "CreateTest",
                "requestBody": {
                  "content": {
                    "application/json": {
                      "schema": { "$ref": "#/components/schemas/TestModel" }
                    }
                  }
                },
                "responses": {
                  "200": { "description": "Success" }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "TestModel": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" },
                  "name": { "type": "string" }
                }
              }
            }
          }
        }
        """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(openApiJson));
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = true
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(stream);
        
        // Should contain TestModel class with JsonConverter attribute
        Assert.Contains("[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof(global::TestApi.TestModelJsonConverter))]", content);
        Assert.Contains("public partial class TestModel", content);
        
        // Should generate TestModelJsonConverter
        Assert.Contains("public class TestModelJsonConverter : global::System.Text.Json.Serialization.JsonConverter<global::TestApi.TestModel>", content);
    }

    [Fact]
    public void JsonConverter_Should_Track_Response_Types()
    {
        // Create a simple OpenAPI spec with response body
        var openApiJson = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/test": {
              "get": {
                "operationId": "GetTest",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": { "$ref": "#/components/schemas/TestResponse" }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "TestResponse": {
                "type": "object",
                "properties": {
                  "result": { "type": "string" },
                  "success": { "type": "boolean" }
                }
              }
            }
          }
        }
        """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(openApiJson));
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = true
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(stream);
        
        // Should contain TestResponse class with JsonConverter attribute
        Assert.Contains("[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof(global::TestApi.TestResponseJsonConverter))]", content);
        Assert.Contains("public partial class TestResponse", content);
        
        // Should generate TestResponseJsonConverter
        Assert.Contains("public class TestResponseJsonConverter : global::System.Text.Json.Serialization.JsonConverter<global::TestApi.TestResponse>", content);
    }

    [Fact]
    public void JsonConverter_Should_Not_Track_Non_Namespace_Types()
    {
        // Create OpenAPI spec that references types outside our namespace
        var openApiJson = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/test": {
              "get": {
                "operationId": "GetTest",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": { "type": "string" }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(openApiJson));
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "TestApi",
                GenerateJsonConverters = true
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(stream);
        
        // Should not generate converters for primitive types
        Assert.DoesNotContain("stringJsonConverter", content);
        Assert.DoesNotContain("System.Text.Json.Serialization.JsonConverter<string>", content);
    }
}