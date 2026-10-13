using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class MediatorSourceGeneratorTests
{
    [Fact]
    public Task GeneratesRegistry_WithSingletonRequestHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult($""Hello {request.Name}"");
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithScopedRequestHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(int Id) : IRequest<int>;

[MediatorScoped]
public class MyRequestHandler : IRequestHandler<MyRequest, int>
{
    public Task<int> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Id * 2);
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithMultipleHandlers()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record Request1(string Name) : IRequest<string>;
public record Request2(int Value) : IRequest<int>;
public record Command1(string Data) : ICommand;

[MediatorSingleton]
public class Request1Handler : IRequestHandler<Request1, string>
{
    public Task<string> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorSingleton]
public class Request2Handler : IRequestHandler<Request2, int>
{
    public Task<int> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

[MediatorScoped]
public class Command1Handler : ICommandHandler<Command1>
{
    public Task Handle(Command1 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithEventHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyEvent(string Message) : IEvent;

[MediatorSingleton]
public class MyEventHandler : IEventHandler<MyEvent>
{
    public Task Handle(MyEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithStreamRequestHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace TestApp;

public record StreamRequest(int Count) : IStreamRequest<int>;

[MediatorSingleton]
public class StreamRequestHandler : IStreamRequestHandler<StreamRequest, int>
{
    public async IAsyncEnumerable<int> Handle(StreamRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return i;
        }
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithRequestMiddleware()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;
public record MyResponse(string Result);

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorSingleton]
public class MyRequestMiddleware : IRequestMiddleware<MyRequest, string>
{
    public async Task<string> Process(IMediatorContext context, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        var result = await next();
        return result.ToUpper();
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithOpenGenericMiddleware()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorSingleton]
public class GenericStreamMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult>
    where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(IMediatorContext context, StreamRequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        return next();
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithCustomRootNamespace()
    {
        var driver = BuildDriverWithOptions(
            @"
using Shiny.Mediator;


public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}",
            rootNamespace: "CustomNamespace");
        
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithPublicAccessModifier()
    {
        var driver = BuildDriverWithOptions(
            @"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}",
            accessModifier: "public");
        
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithCustomMethodName()
    {
        var driver = BuildDriverWithOptions(
            @"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}",
            methodName: "AddCustomMediatorServices");
        
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithMixedLifetimes()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record Request1(string Name) : IRequest<string>;
public record Request2(int Value) : IRequest<int>;

[MediatorSingleton]
public class Request1Handler : IRequestHandler<Request1, string>
{
    public Task<string> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorScoped]
public class Request2Handler : IRequestHandler<Request2, int>
{
    public Task<int> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

//     [Fact]
//     public Task DoesNotGenerate_WithoutAttribute()
//     {
//         var driver = BuildDriver(@"
// using Shiny.Mediator;
//
// namespace TestApp;
//
// public record MyRequest(string Name) : IRequest<string>;
//
// [MediatorSingleton]
// public class MyRequestHandler : IRequestHandler<MyRequest, string>
// {
//     public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
//         => Task.FromResult(request.Name);
// }");
//         var result = driver.GetRunResult().Results.FirstOrDefault();
//         result.Exception.ShouldBeNull();
//         // Should only generate the attributes file, no registry
//         result.GeneratedSources.Length.ShouldBe(1);
//         result.GeneratedSources[0].HintName.ShouldBe("MediatorAttributes.g.cs");
//         return Verify(result);
//     }

    [Fact(DisplayName = "Resolver covers nested in-assembly property types transitively (RideTime → Position)")]
    public Task GeneratesJsonResolver_WithTransitiveNestedTypes()
    {
        // Auto request/result JSON serialization is opt-in (ShinyMediatorGenerateJsonContext=true)
        var driver = BuildDriverWithOptions(@"
using System.Collections.Generic;
using Shiny.Mediator;


namespace TestApp;

public record GetRides : IRequest<IReadOnlyList<RideTime>>;

public record RideTime(string Id, string Name, Position? Position);

public record Position(double Latitude, double Longitude);

[MediatorSingleton]
public class GetRidesHandler : IRequestHandler<GetRides, IReadOnlyList<RideTime>>
{
    public Task<IReadOnlyList<RideTime>> Handle(GetRides request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<RideTime>>(new List<RideTime>());
}", generateJsonContext: true);
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }


    [Fact]
    public Task DoesNotGenerate_WithoutMediatorAttribute()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

// No [MediatorSingleton] or [MediatorScoped] attribute
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        // Should only generate the attributes file, no registry
        result.GeneratedSources.Length.ShouldBe(1);
        result.GeneratedSources[0].HintName.ShouldBe("MediatorAttributes.g.cs");
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithCommandMiddleware()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyCommand(string Data) : ICommand;

[MediatorSingleton]
public class MyCommandHandler : ICommandHandler<MyCommand>
{
    public Task Handle(MyCommand request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

[MediatorSingleton]
public class MyCommandMiddleware : ICommandMiddleware<MyCommand>
{
    public async Task Process(IMediatorContext context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        await next();
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    const string ContractKeySource = @"
using System;
using Shiny.Mediator;


namespace TestApp;

public enum Status { Active, Closed }

public record GetOrders(string? Customer, int Page, Status Status, DateTime? Since, string? Unused) : IRequest<string>;

public class OrderStream : IStreamRequest<int>
{
    public int Take { get; set; }
    public string WriteOnly { set { } }
    public string this[int index] => string.Empty;
}

public record SaveOrder(Guid Id) : ICommand;

// supplies its own key - never in the generated source
public record KeyedRequest(string Id) : IRequest<string>, IContractKey
{
    public string GetKey() => Id;
}

public class Outer
{
    public record NestedRequest(int Value) : IRequest<string>;
}

public abstract record BaseQuery(string Tenant) : IRequest<string>;
public record DerivedQuery(string Tenant, string Term) : BaseQuery(Tenant);

[MediatorSingleton]
public class GetOrdersHandler : IRequestHandler<GetOrders, string>
{
    public Task<string> Handle(GetOrders request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(string.Empty);
}";


    [Fact(DisplayName = "Contract keys - opt-in generates a reflection-free IContractKeySource and registers it")]
    public Task GeneratesContractKeySource()
    {
        var driver = BuildDriverWithOptions(ContractKeySource, generateContractKeys: true);
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }


    [Fact(DisplayName = "Contract keys - nothing generated when not opted in")]
    public void DoesNotGenerateContractKeySource_ByDefault()
    {
        var driver = BuildDriver(ContractKeySource);
        var result = driver.GetRunResult().Results.First();
        result.GeneratedSources.ShouldNotContain(x => x.HintName.Contains("GeneratedContractKeySource"));
        result.GeneratedSources.Single(x => x.HintName.EndsWith("_Registry.g.cs"))
            .SourceText.ToString()
            .ShouldNotContain("IContractKeySource");
    }


    [Fact(DisplayName = "Contract keys - generated keys match the reflection fallback exactly")]
    public void GeneratedContractKeys_MatchReflectionKeys()
    {
        var driver = BuildDriverWithOptions(ContractKeySource, generateContractKeys: true);
        var keySourceTree = driver
            .GetRunResult()
            .GeneratedTrees
            .Single(x => x.FilePath.EndsWith("GeneratedContractKeySource.g.cs"));

        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(x => !x.IsDynamic && !String.IsNullOrEmpty(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        var compilation = CSharpCompilation.Create(
            "ContractKeyParity",
            [
                CSharpSyntaxTree.ParseText(ContractKeySource.Replace("[MediatorSingleton]", String.Empty)),
                CSharpSyntaxTree.ParseText("global using System.Threading; global using System.Threading.Tasks;"),
                keySourceTree
            ],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);
        emit.Success.ShouldBeTrue(String.Join(Environment.NewLine, emit.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

        var assembly = Assembly.Load(ms.ToArray());
        var source = (IContractKeySource)Activator.CreateInstance(assembly.GetType("TestAssembly.GeneratedContractKeySource")!)!;
        var reflectionProvider = new DefaultContractKeyProvider();

        object Create(string typeName, params object?[] args)
            => Activator.CreateInstance(assembly.GetType(typeName)!, args)!;

        var statusClosed = Enum.Parse(assembly.GetType("TestApp.Status")!, "Closed");
        var contracts = new[]
        {
            Create("TestApp.GetOrders", "allan", 2, statusClosed, new DateTime(2026, 10, 13), null),
            Create("TestApp.GetOrders", null, 0, statusClosed, null, null),
            Create("TestApp.SaveOrder", Guid.Parse("8f3c2a5e-6a7b-4c1d-9e2f-0a1b2c3d4e5f")),
            Create("TestApp.Outer+NestedRequest", 7),
            Create("TestApp.DerivedQuery", "t1", "shoes")
        };
        foreach (var contract in contracts)
        {
            source.TryGetKey(contract, out var generatedKey).ShouldBeTrue(contract.GetType().FullName);
            generatedKey.ShouldBe(reflectionProvider.GetContractKey(contract));
        }

        // indexers make reflection throw, so the type is left out and falls through
        var stream = Create("TestApp.OrderStream");
        source.TryGetKey(stream, out _).ShouldBeFalse();
    }


    static GeneratorDriver BuildDriver(string sourceCode)
        => BuildDriverWithOptions(sourceCode);

    static GeneratorDriver BuildDriverWithOptions(
        string sourceCode,
        string? rootNamespace = null,
        string? accessModifier = null,
        string? methodName = null,
        bool generateJsonContext = false,
        bool generateContractKeys = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        
        // Add minimal references for compilation
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        };

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], references, options);

        var generator = new MediatorSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Add MSBuild options if specified
        if (rootNamespace != null || accessModifier != null || methodName != null || generateJsonContext || generateContractKeys)
        {
            var optionsProvider = new TestAnalyzerConfigOptionsProvider(
                rootNamespace,
                accessModifier,
                methodName,
                generateJsonContext,
                generateContractKeys);
            driver = (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);
        }

        return driver.RunGenerators(compilation);
    }

    class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestAnalyzerConfigOptions globalOptions;

        public TestAnalyzerConfigOptionsProvider(
            string? rootNamespace,
            string? accessModifier,
            string? methodName,
            bool generateJsonContext,
            bool generateContractKeys)
        {
            this.globalOptions = new TestAnalyzerConfigOptions(
                rootNamespace,
                accessModifier,
                methodName,
                generateJsonContext,
                generateContractKeys);
        }

        public override AnalyzerConfigOptions GlobalOptions => this.globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.globalOptions;
    }

    class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> options = new();

        public TestAnalyzerConfigOptions(
            string? rootNamespace,
            string? accessModifier,
            string? methodName,
            bool generateJsonContext,
            bool generateContractKeys)
        {
            if (rootNamespace != null)
                this.options["build_property.RootNamespace"] = rootNamespace;
            if (accessModifier != null)
                this.options["build_property.ShinyMediatorRegistryAccessModifier"] = accessModifier;
            if (methodName != null)
                this.options["build_property.ShinyMediatorRegistryMethodName"] = methodName;
            if (generateJsonContext)
                this.options["build_property.ShinyMediatorGenerateJsonContext"] = "true";
            if (generateContractKeys)
                this.options["build_property.ShinyMediatorGenerateContractKeys"] = "true";
        }

        public override bool TryGetValue(string key, out string value)
            => this.options.TryGetValue(key, out value!);
    }
}