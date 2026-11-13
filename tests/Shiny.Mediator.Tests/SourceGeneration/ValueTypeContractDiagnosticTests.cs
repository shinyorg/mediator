using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


/// <summary>
/// SHINYMED010: under Native AOT, MS.DI refuses to close open-generic middleware over a value type
/// (CallSiteFactory.VerifyOpenGenericAotCompatibility) and the first call of such a contract throws.
/// The generator warns at build time when the project publishes AOT or is marked AOT compatible.
/// </summary>
public class ValueTypeContractDiagnosticTests
{
    const string Handlers = """
        using Shiny.Mediator;

        namespace TestApp;

        public record IsOpen : IRequest<bool>;
        public record GetName : IRequest<string>;
        public record struct Ping : ICommand;

        [MediatorSingleton]
        public class IsOpenHandler : IRequestHandler<IsOpen, bool>
        {
            public Task<bool> Handle(IsOpen request, IMediatorContext context, CancellationToken cancellationToken) => Task.FromResult(true);
        }

        [MediatorSingleton]
        public class GetNameHandler : IRequestHandler<GetName, string>
        {
            public Task<string> Handle(GetName request, IMediatorContext context, CancellationToken cancellationToken) => Task.FromResult("name");
        }

        [MediatorSingleton]
        public class PingHandler : ICommandHandler<Ping>
        {
            public Task Handle(Ping command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
        }
        """;


    [Theory]
    [InlineData("build_property.PublishAot")]
    [InlineData("build_property.IsAotCompatible")]
    public void Warns_For_Value_Type_Results_And_Contracts_Under_Native_Aot(string property)
    {
        var diagnostics = Run(new() { [property] = "true" });

        diagnostics.Select(x => (x.Id, x.Severity, Handler: x.Location.SourceTree?.GetText().ToString(x.Location.SourceSpan))).ShouldBe([
            ("SHINYMED010", DiagnosticSeverity.Warning, "IsOpenHandler"),
            ("SHINYMED010", DiagnosticSeverity.Warning, "PingHandler")
        ], ignoreOrder: true);
        diagnostics.Single(x => x.GetMessage().Contains("IsOpenHandler")).GetMessage().ShouldContain("'bool'");
    }


    [Fact]
    public void Stays_Quiet_Without_Native_Aot()
        => Run([]).ShouldBeEmpty();


    static Diagnostic[] Run(Dictionary<string, string> buildProperties)
    {
        var tree = CSharpSyntaxTree.ParseText(Handlers);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
        };
        var compilation = CSharpCompilation.Create("TestAssembly", [tree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return CSharpGeneratorDriver.Create(new MediatorSourceGenerator())
            .WithUpdatedAnalyzerConfigOptions(new MockAnalyzerConfigOptionsProvider(buildProperties))
            .RunGenerators(compilation)
            .GetRunResult()
            .Diagnostics
            .Where(x => x.Id == "SHINYMED010")
            .ToArray();
    }
}
