using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;

public class NamespaceWithDotsTests
{
    [Fact]
    public void NamespaceWithDots_PreservesNamespace()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
using Shiny.Mediator;

namespace ClassLibrary.WithPoint;

[SingletonHandler]
public class TestHandler : ICommandHandler<TestCommand>
{
    public Task Handle(TestCommand command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

public record TestCommand : ICommand;
");
        
        var metadataReference = MetadataReference.CreateFromFile(typeof(ICommand).Assembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        
        // Test case 1: When assembly name has dots and RootNamespace is not specified
        var compilation1 = CSharpCompilation.Create("ClassLibraryWithPoint", [syntaxTree], [metadataReference], options);
        var dict1 = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var provider1 = new MockAnalyzerConfigOptionsProvider(dict1);
        var driver1 = CSharpGeneratorDriver.Create([new MediatorSourceGenerator().AsSourceGenerator()], optionsProvider: provider1);
        var runResult1 = driver1.RunGenerators(compilation1);
        
        var generatedSources1 = runResult1.Results[0].GeneratedSources;
        var registrationSource1 = generatedSources1.FirstOrDefault(s => s.HintName.Contains("MediatorHandlersRegistration"));
        var sourceText1 = registrationSource1.SourceText.ToString();
        
        // When assembly name doesn't have dots, namespace should be "ClassLibraryWithPoint"
        sourceText1.ShouldContain("namespace ClassLibraryWithPoint;");
        
        // Test case 2: When RootNamespace is specified with dots
        var compilation2 = CSharpCompilation.Create("ClassLibraryWithPoint", [syntaxTree], [metadataReference], options);
        var dict2 = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["build_property.RootNamespace"] = "ClassLibrary.WithPoint"
        };
        var provider2 = new MockAnalyzerConfigOptionsProvider(dict2);
        var driver2 = CSharpGeneratorDriver.Create([new MediatorSourceGenerator().AsSourceGenerator()], optionsProvider: provider2);
        var runResult2 = driver2.RunGenerators(compilation2);
        
        var generatedSources2 = runResult2.Results[0].GeneratedSources;
        var registrationSource2 = generatedSources2.FirstOrDefault(s => s.HintName.Contains("MediatorHandlersRegistration"));
        var sourceText2 = registrationSource2.SourceText.ToString();
        
        // When RootNamespace is specified with dots, it should be preserved
        sourceText2.ShouldContain("namespace ClassLibrary.WithPoint;");
    }
}