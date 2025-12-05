using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class AttributeMiddlewareSourceGeneratorTests
{
    [Fact]
    public Task PartialClassWithMultipleDeclarations_GeneratesOnce()
    {
        var driver = BuildDriver(@"
using System.Threading;
using System.Threading.Tasks;
using Shiny.Mediator;

public record ConnectivityChanged(bool Connected) : IEvent;

public interface IConnectivityEventHandler : IEventHandler<ConnectivityChanged>;

public abstract partial class ViewModel
{
    // Empty partial - simulates primary constructor partial
}

public abstract partial class ViewModel : IConnectivityEventHandler
{
    [OfflineAvailable]
    public Task Handle(ConnectivityChanged @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        // Should generate exactly one file for the merged partial class
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    static GeneratorDriver BuildDriver(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEvent).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OfflineAvailableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        };

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], references, options);

        var generator = new AttributeMarkerSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
    }

    [Fact]
    public void SingleRequestHandlerWithSingleAttribute()
    {
        var handler = new SingleRequestTestHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        var attr = marker.GetAttribute<CacheAttribute>(new SingleRequest(1));
        attr.ShouldNotBeNull();
        attr.AbsoluteExpirationSeconds.ShouldBe(10);
        attr.SlidingExpirationSeconds.ShouldBe(20);
    }
    
    [Fact]
    public void MultipleRequestHandlersInSameClass()
    {
        var handler = new MultiHandlerTestHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        // Test first request handler
        var attr1 = marker.GetAttribute<CacheAttribute>(new FirstRequest());
        attr1.ShouldNotBeNull();
        attr1.AbsoluteExpirationSeconds.ShouldBe(30);
        
        // Test second request handler
        var attr2 = marker.GetAttribute<CacheAttribute>(new SecondRequest());
        attr2.ShouldNotBeNull();
        attr2.SlidingExpirationSeconds.ShouldBe(40);
        
        // Test command handler
        var attr3 = marker.GetAttribute<OfflineAvailableAttribute>(new TestCommand());
        attr3.ShouldNotBeNull();
    }
    
    [Fact]
    public void MultipleAttributesOnSingleHandler()
    {
        var handler = new MultiAttributeHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        var cacheAttr = marker.GetAttribute<CacheAttribute>(new MultiAttrRequest());
        cacheAttr.ShouldNotBeNull();
        cacheAttr.AbsoluteExpirationSeconds.ShouldBe(50);
        
        var offlineAttr = marker.GetAttribute<OfflineAvailableAttribute>(new MultiAttrRequest());
        offlineAttr.ShouldNotBeNull();
    }
    
    [Fact]
    public void StreamRequestHandlerWithAttribute()
    {
        var handler = new StreamTestHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        var attr = marker.GetAttribute<ReplayStreamAttribute>(new StreamRequest());
        attr.ShouldNotBeNull();
    }
    
    [Fact]
    public void EventHandlerWithAttribute()
    {
        var handler = new EventTestHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        var attr = marker.GetAttribute<CacheAttribute>(new TestEvent());
        attr.ShouldNotBeNull();
        attr.AbsoluteExpirationSeconds.ShouldBe(60);
    }
    
    [Fact]
    public void CommandHandlerWithAttribute()
    {
        var handler = new CommandTestHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        var attr = marker.GetAttribute<OfflineAvailableAttribute>(new SingleCommand());
        attr.ShouldNotBeNull();
    }
    
    [Fact]
    public void NoAttributeReturnsNull()
    {
        var handler = new SingleRequestTestHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        // Request a different message type
        var attr = marker.GetAttribute<CacheAttribute>(new FirstRequest());
        attr.ShouldBeNull();
    }
    
    [Fact]
    public void WrongAttributeTypeReturnsNull()
    {
        var handler = new SingleRequestTestHandler();
        var marker = handler as IHandlerAttributeMarker;
        marker.ShouldNotBeNull();
        
        // Request wrong attribute type
        var attr = marker.GetAttribute<OfflineAvailableAttribute>(new SingleRequest(1));
        attr.ShouldBeNull();
    }
}

// Test records and handlers
public record SingleRequest(int Id) : IRequest<string>;

public partial class SingleRequestTestHandler : IRequestHandler<SingleRequest, string>
{
    [Cache(AbsoluteExpirationSeconds = 10, SlidingExpirationSeconds = 20)]
    public Task<string> Handle(SingleRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult("test");
    }
}

// Multiple handlers in same class
public record FirstRequest : IRequest<string>;
public record SecondRequest : IRequest<int>;
public record TestCommand : ICommand;

public partial class MultiHandlerTestHandler : 
    IRequestHandler<FirstRequest, string>,
    IRequestHandler<SecondRequest, int>,
    ICommandHandler<TestCommand>
{
    [Cache(AbsoluteExpirationSeconds = 30)]
    public Task<string> Handle(FirstRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult("first");
    }
    
    [Cache(SlidingExpirationSeconds = 40)]
    public Task<int> Handle(SecondRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(42);
    }
    
    [OfflineAvailable]
    public Task Handle(TestCommand request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// Multiple attributes on single handler
public record MultiAttrRequest : IRequest<string>;

public partial class MultiAttributeHandler : IRequestHandler<MultiAttrRequest, string>
{
    [Cache(AbsoluteExpirationSeconds = 50)]
    [OfflineAvailable]
    public Task<string> Handle(MultiAttrRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult("multi");
    }
}

// Stream handler
public record StreamRequest : IStreamRequest<string>;

public partial class StreamTestHandler : IStreamRequestHandler<StreamRequest, string>
{
    [ReplayStream]
    public async IAsyncEnumerable<string> Handle(StreamRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        yield return "item";
    }
}

// Event handler
public record TestEvent : IEvent;

public partial class EventTestHandler : IEventHandler<TestEvent>
{
    [Cache(AbsoluteExpirationSeconds = 60)]
    public Task Handle(TestEvent request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// Command handler
public record SingleCommand : ICommand;

public partial class CommandTestHandler : ICommandHandler<SingleCommand>
{
    [OfflineAvailable]
    public Task Handle(SingleCommand request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}