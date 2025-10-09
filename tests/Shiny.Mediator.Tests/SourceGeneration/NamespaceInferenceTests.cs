using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;

public class NamespaceInferenceTests
{
    [Fact]
    public void InferNamespaceFromHandlers_WhenNoRootNamespaceProvided()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
using Shiny.Mediator;

namespace ClassLibrary.WithPoint.Handlers;

[SingletonHandler]
public class TestHandler1 : ICommandHandler<TestCommand>
{
    public Task Handle(TestCommand command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

public record TestCommand : ICommand;

namespace ClassLibrary.WithPoint.Services;

[SingletonHandler]  
public class TestHandler2 : ICommandHandler<TestCommand2>
{
    public Task Handle(TestCommand2 command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

public record TestCommand2 : ICommand;
");
        
        var metadataReference = MetadataReference.CreateFromFile(typeof(ICommand).Assembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        
        // Create compilation with assembly name without dots
        var compilation = CSharpCompilation.Create("ClassLibraryWithPoint", [syntaxTree], [metadataReference], options);
        
        var generator = new MediatorSourceGenerator().AsSourceGenerator();
        
        // No RootNamespace provided
        var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var provider = new MockAnalyzerConfigOptionsProvider(dict);
    
        var driver = CSharpGeneratorDriver.Create([generator], optionsProvider: provider);
        var runResult = driver.RunGenerators(compilation);
        
        // Get the generated sources
        var generatedSources = runResult.Results[0].GeneratedSources;
        
        // Find the registration file
        var registrationSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("MediatorHandlersRegistration"));
        registrationSource.SourceText.ToString().ShouldNotBeNull();
        
        // Check that namespace is inferred from handlers (common namespace)
        var sourceText = registrationSource.SourceText.ToString();
        sourceText.ShouldContain("namespace ClassLibrary.WithPoint;");
    }
    
    [Fact]
    public void UsesSingleHandlerNamespace_WhenAllHandlersInSameNamespace()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
using Shiny.Mediator;

namespace My.Special.Namespace;

[SingletonHandler]
public class Handler1 : ICommandHandler<Command1>
{
    public Task Handle(Command1 command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

[SingletonHandler]
public class Handler2 : ICommandHandler<Command2>  
{
    public Task Handle(Command2 command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

public record Command1 : ICommand;
public record Command2 : ICommand;
");
        
        var metadataReference = MetadataReference.CreateFromFile(typeof(ICommand).Assembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        
        // Assembly name is different from namespace
        var compilation = CSharpCompilation.Create("SomeAssembly", [syntaxTree], [metadataReference], options);
        
        var generator = new MediatorSourceGenerator().AsSourceGenerator();
        var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var provider = new MockAnalyzerConfigOptionsProvider(dict);
    
        var driver = CSharpGeneratorDriver.Create([generator], optionsProvider: provider);
        var runResult = driver.RunGenerators(compilation);
        
        var generatedSources = runResult.Results[0].GeneratedSources;
        var registrationSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("MediatorHandlersRegistration"));
        var sourceText = registrationSource.SourceText.ToString();
        
        // Should use the handler's namespace
        sourceText.ShouldContain("namespace My.Special.Namespace;");
    }
}