using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.Http;


public class HttpClientSourceGeneratorTests
{

    [Fact]
    public Task Generate_SimpleGetRequest_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/users")]
            public class GetUsersRequest : IRequest<UserListResult>
            {
            }
            
            public class UserListResult
            {
                public List<string> Users { get; set; } = new();
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_GetRequestWithRouteParameter_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/users/{UserId}")]
            public class GetUserRequest : IRequest<UserResult>
            {
                public int UserId { get; set; }
            }
            
            public class UserResult
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_GetRequestWithQueryParameter_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/search")]
            public class SearchRequest : IRequest<SearchResult>
            {
                [Query("query")]
                public string SearchTerm { get; set; } = string.Empty;
            }
            
            public class SearchResult
            {
                public int Count { get; set; }
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_PostRequestWithBody_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Post("/api/users")]
            public class CreateUserRequest : IRequest<UserResult>
            {
                [Body]
                public CreateUserBody? Body { get; set; }
            }
            
            public class CreateUserBody
            {
                public string Name { get; set; } = string.Empty;
                public string Email { get; set; } = string.Empty;
            }
            
            public class UserResult
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_RequestWithHeaders_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/data")]
            public class GetDataRequest : IRequest<DataResult>
            {
                [Header("X-Api-Key")]
                public string ApiKey { get; set; } = string.Empty;
                
                [Header]
                public string Authorization { get; set; } = string.Empty;
            }
            
            public class DataResult
            {
                public string Data { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_ComplexRequestWithAllFeatures_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Post("/api/orders/{OrderId}")]
            public class UpdateOrderRequest : IRequest<OrderResult>
            {
                public int OrderId { get; set; }
                
                [Query("status")]
                public string Status { get; set; } = string.Empty;
                
                [Header("X-Custom-Header")]
                public string CustomHeader { get; set; } = string.Empty;
                
                [Header]
                public string? Authorization { get; set; }
                
                [Body]
                public UpdateOrderBody? Body { get; set; }
            }
            
            public class UpdateOrderBody
            {
                public decimal Amount { get; set; }
                public string Notes { get; set; } = string.Empty;
            }
            
            public class OrderResult
            {
                public int Id { get; set; }
                public string Status { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_StreamRequest_ShouldGenerateStreamHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/events")]
            public class StreamEventsRequest : IStreamRequest<EventItem>
            {
            }
            
            public class EventItem
            {
                public string Message { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_StreamRequestWithServerSentEvents_ShouldGenerateStreamHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/sse")]
            public class ServerSentEventsRequest : IStreamRequest<EventItem>, IServerSentEventsStream
            {
            }
            
            public class EventItem
            {
                public string Data { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_MultipleRequests_ShouldGenerateAllHandlersAndRegistration()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/users")]
            public class GetUsersRequest : IRequest<UserListResult>
            {
            }
            
            [Post("/api/users")]
            public class CreateUserRequest : IRequest<UserResult>
            {
                [Body]
                public CreateUserBody? Body { get; set; }
            }
            
            [Put("/api/users/{UserId}")]
            public class UpdateUserRequest : IRequest<UserResult>
            {
                public int UserId { get; set; }
                
                [Body]
                public UpdateUserBody? Body { get; set; }
            }
            
            [Delete("/api/users/{UserId}")]
            public class DeleteUserRequest : IRequest<DeleteResult>
            {
                public int UserId { get; set; }
            }
            
            public class UserListResult
            {
                public List<string> Users { get; set; } = new();
            }
            
            public class UserResult
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }
            
            public class CreateUserBody
            {
                public string Name { get; set; } = string.Empty;
            }
            
            public class UpdateUserBody
            {
                public string Name { get; set; } = string.Empty;
            }
            
            public class DeleteResult
            {
                public bool Success { get; set; }
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_PutRequest_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Put("/api/settings")]
            public class UpdateSettingsRequest : IRequest<SettingsResult>
            {
                [Body]
                public SettingsBody? Body { get; set; }
            }
            
            public class SettingsBody
            {
                public bool Enabled { get; set; }
            }
            
            public class SettingsResult
            {
                public string Status { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_PatchRequest_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Patch("/api/users/{UserId}")]
            public class PatchUserRequest : IRequest<UserResult>
            {
                public int UserId { get; set; }
                
                [Body]
                public PatchBody? Body { get; set; }
            }
            
            public class PatchBody
            {
                public string? Name { get; set; }
            }
            
            public class UserResult
            {
                public int Id { get; set; }
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_DeleteRequest_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Delete("/api/items/{ItemId}")]
            public class DeleteItemRequest : IRequest<DeleteResult>
            {
                public int ItemId { get; set; }
            }
            
            public class DeleteResult
            {
                public bool Success { get; set; }
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_PocTestsRequest_ShouldGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/poc/test/{RouteParameter}")]
            public class PocTests : IRequest<ResultObject>
            {
                public int RouteParameter { get; set; }
                
                [Header("X-Custom-Header")] 
                public string TestValue { get; set; } = string.Empty;
                
                [Header]
                public string? Test2Value { get; set; }
            
                [Query("TestQuery")]
                public int? QueryValue { get; set; }
            
                [Body]
                public TestBody? Body { get; set; }
            }
            
            public class TestBody
            {
                public string Hello { get; set; } = string.Empty;
            }
            
            public class ResultObject
            {
                public string World { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public Task Generate_PocStreamRequest_ShouldGenerateStreamHandler()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/poc/stream/")]
            public class PocStreamRequest : IStreamRequest<ResultObject>;
            
            public class ResultObject
            {
                public string World { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    [Fact]
    public void Generate_MultipleBodyAttributes_ShouldReportError()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Post("/api/test")]
            public class TestRequest : IRequest<TestResult>
            {
                [Body]
                public TestBody1? Body1 { get; set; }
                
                [Body]
                public TestBody2? Body2 { get; set; }
            }
            
            public class TestBody1
            {
                public string Value1 { get; set; } = string.Empty;
            }
            
            public class TestBody2
            {
                public string Value2 { get; set; } = string.Empty;
            }
            
            public class TestResult
            {
                public string Value { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        
        // Should report diagnostic error
        result.Diagnostics.ShouldNotBeEmpty();
        result.Diagnostics[0].Id.ShouldBe("SHINYMED_HTTP002");
    }

    [Fact]
    public void Generate_MissingRouteParameterProperty_ShouldReportError()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/users/{UserId}")]
            public class GetUserRequest : IRequest<UserResult>
            {
                // Missing UserId property!
            }
            
            public class UserResult
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        
        // Should report diagnostic error
        result.Diagnostics.ShouldNotBeEmpty();
        result.Diagnostics[0].Id.ShouldBe("SHINYMED_HTTP003");
    }

    [Fact]
    public void Generate_ClassWithoutHttpAttribute_ShouldNotGenerateHandler()
    {
        var source = """
            using Shiny.Mediator;
            
            namespace TestNamespace;
            
            public class NoAttributeRequest : IRequest<TestResult>
            {
            }
            
            public class TestResult
            {
                public string Value { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        
        // Should not generate any handlers
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void Generate_ClassWithoutMediatorInterface_ShouldReportError()
    {
        var source = """
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/test")]
            public class NoInterfaceRequest
            {
            }
            """;

        var result = RunGenerator(source);
        
        // Should report diagnostic error
        result.Diagnostics.ShouldNotBeEmpty();
        result.Diagnostics[0].Id.ShouldBe("SHINYMED_HTTP001");
    }

    [Fact]
    public Task Generate_PropertyWithoutAttributes_NotInRoute_ShouldBeIgnored()
    {
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/users/{UserId}")]
            public class GetUserRequest : IRequest<UserResult>
            {
                public int UserId { get; set; }
                
                // This property should be ignored (not added as query param automatically)
                public string? IgnoredProperty { get; set; }
            }
            
            public class UserResult
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        var result = RunGenerator(source);
        return Verify(result);
    }

    static GeneratorRunResult RunGenerator(string source, string? rootNamespace = null, string? httpNamespace = null)
    {
        var compilation = CreateCompilation(source);
        var generator = new HttpClientSourceGenerator().AsSourceGenerator();
        
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            rootNamespace ?? "TestNamespace",
            httpNamespace
        );
        
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            optionsProvider: optionsProvider
        );
        
        var runDriver = driver.RunGenerators(compilation);
        return runDriver.GetRunResult().Results[0];
    }

    static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [
                MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ICommand).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MediatorHttpGroupAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

// Test helper class for providing analyzer config options
class TestAnalyzerConfigOptionsProvider(string rootNamespace, string? httpNamespace = null) : AnalyzerConfigOptionsProvider
{
    private readonly TestAnalyzerConfigOptions options = new(rootNamespace, httpNamespace);

    public override AnalyzerConfigOptions GlobalOptions => options;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
}

class TestAnalyzerConfigOptions(string rootNamespace, string? httpNamespace) : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> options = new()
    {
        { "build_property.RootNamespace", rootNamespace },
        { "build_property.ShinyMediatorHttpNamespace", httpNamespace ?? rootNamespace }
    };

    public override bool TryGetValue(string key, out string value)
    {
        return options.TryGetValue(key, out value!);
    }
}

