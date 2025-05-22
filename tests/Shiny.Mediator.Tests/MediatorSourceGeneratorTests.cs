using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests;

public class MediatorSourceGeneratorTests
{
    [Fact]
    public void DidRegister()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator();
        services.AddDiscoveredMediatorHandlersFromUnitTests(); // assembly name changed to get around assembly name (Shiny.Mediator) detection
        var sp = services.BuildServiceProvider();

        sp.GetService<IEventHandler<SourceGenEvent>>().ShouldNotBeNull("Event Handler not found");
        sp.GetService<ICommandHandler<SourceGenCommand>>().ShouldNotBeNull("Command Handler not found");
        sp.GetService<IRequestHandler<SourceGenResponseRequest, SourceGenResponse>>().ShouldNotBeNull("Request/Response Handler not found");
    }
    
    
    [Fact]
    public Task Driver_Success()
    {
        var driver = BuildDriver(this.GetType().Assembly);
        return Verify(driver);
    }
    
    
    [Fact]
    public Task RunResult_Success()
    {
        var driver = BuildDriver(this.GetType().Assembly);
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(2);
        return Verify(result);
    }
    
    
    [Fact]
    public Task RunResult_Disabled()
    {
        var driver = BuildDriver(this.GetType().Assembly, ("ShinyMediatorDisableSourceGen", "true"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }
    
    
    static GeneratorDriver BuildDriver(Assembly metadataAssembly, params IEnumerable<(string Key, string Value)> buildProperties)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
using Shiny.Mediator;

namespace MyTests;

public record SourceGenCommand : ICommand;

[SingletonHandler]
public class SourceGenCommandHandler : ICommandHandler<SourceGenCommand>
{
    public Task Handle(SourceGenCommand command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}");
        
        var metadataReference = MetadataReference.CreateFromFile(metadataAssembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], [metadataReference], options);
        // add code

        var generator = new MediatorSourceGenerator();
        
        var dict = buildProperties.ToDictionary(x => "build_property." + x.Key, x => x.Value, comparer: StringComparer.InvariantCultureIgnoreCase);
        var provider = new MockAnalyzerConfigOptionsProvider(dict);
    
        var driver = CSharpGeneratorDriver.Create([generator], optionsProvider: provider);
        return driver.RunGenerators(compilation);
    }
}

public record SourceGenCommand : ICommand;
public record SourceGenResponseRequest : IRequest<SourceGenResponse>;
public record SourceGenResponse;
public record SourceGenEvent : IEvent;


[SingletonHandler]
public class SourceGenCommandHandler : ICommandHandler<SourceGenCommand>
{
    public Task Handle(SourceGenCommand command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}
[SingletonHandler]
public class SourceGenResponseRequestHandler : IRequestHandler<SourceGenResponseRequest, SourceGenResponse>
{
    public Task<SourceGenResponse> Handle(SourceGenResponseRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new SourceGenResponse());
}
[SingletonHandler]
public class SourceGenEventHandler : IEventHandler<SourceGenEvent>
{
    public Task Handle(SourceGenEvent @event, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}