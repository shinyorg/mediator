using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class MediatorAIToolSourceGeneratorTests
{
    [Fact]
    public Task Generates_RequestWithAllPrimitiveTypes()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Search with all primitive types")]
            public record PrimitivesRequest(
                string Name,
                bool Active,
                byte Age,
                sbyte SignedByte,
                short SmallNumber,
                ushort UnsignedSmall,
                int Count,
                uint UnsignedCount,
                long BigNumber,
                ulong UnsignedBig,
                float Rate,
                double Precise,
                decimal Price
            ) : IRequest<string>;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task Generates_RequestWithDateTimeTypes()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Search with date/time types")]
            public record DateTimeRequest(
                DateTime When,
                DateTimeOffset WhenOffset,
                DateOnly JustDate,
                TimeOnly JustTime,
                TimeSpan Duration
            ) : IRequest<string>;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task Generates_RequestWithGuidAndUri()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Search by identifier")]
            public record IdentifierRequest(
                Guid Id,
                Uri Endpoint
            ) : IRequest<string>;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task Generates_RequestWithEnum()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            public enum Priority { Low, Medium, High }

            [Description("Search with enum")]
            public record EnumRequest(Priority Level) : IRequest<string>;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task Generates_RequestWithNullableTypes()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Search with nullable types")]
            public record NullableRequest(
                string? Name,
                int? Count,
                DateTime? When,
                Guid? Id
            ) : IRequest<string>;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task Generates_RequestWithDefaults()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Search with default values")]
            public record DefaultsRequest(
                string Name,
                int Page = 1,
                bool Active = true,
                string Sort = "name"
            ) : IRequest<string>;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task Generates_CommandContract()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Delete a resource")]
            public record DeleteCommand(Guid Id) : ICommand;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task Generates_RequestWithPropertyDescriptions()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Search for users")]
            public class SearchUsersRequest : IRequest<string>
            {
                [Description("The search term to filter by")]
                public string Query { get; set; }

                [Description("Maximum number of results")]
                public int Limit { get; set; }
            }
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public void DoesNotGenerate_WithoutDescriptionAttribute()
    {
        var result = RunGenerator("""
            using System;
            using Shiny.Mediator;

            namespace TestApp;

            public record NoDescriptionRequest(string Name) : IRequest<string>;
            """);

        result.Exception.ShouldBeNull();
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void DoesNotGenerate_WhenFeatureDisabled()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Should not generate")]
            public record SomeRequest(string Name) : IRequest<string>;
            """, enabled: false);

        result.Exception.ShouldBeNull();
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public Task Generates_MultipleContracts_WithRegistration()
    {
        var result = RunGenerator("""
            using System;
            using System.ComponentModel;
            using Shiny.Mediator;

            namespace TestApp;

            [Description("Get a thing")]
            public record GetThingRequest(int Id) : IRequest<string>;

            [Description("Delete a thing")]
            public record DeleteThingCommand(int Id) : ICommand;
            """);

        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    static GeneratorRunResult RunGenerator(string source, bool enabled = true)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.AI.AITool).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new MediatorAIToolSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var buildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyMediatorGenerateAITools"] = enabled ? "true" : "false"
        };
        var optionsProvider = new MockAnalyzerConfigOptionsProvider(buildProperties);
        driver = (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);

        var result = driver.RunGenerators(compilation).GetRunResult();
        return result.Results.First();
    }
}
