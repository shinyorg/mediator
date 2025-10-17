namespace Shiny.Mediator.Tests.SourceGeneration;


public class AttributeMiddlewareSourceGeneratorTests
{
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

