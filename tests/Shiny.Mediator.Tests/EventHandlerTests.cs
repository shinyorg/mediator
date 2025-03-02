using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class EventHandlerTests
{
    readonly ITestOutputHelper helper;
    public EventHandlerTests(ITestOutputHelper helper)
    {
        CatchAllEventHandler.Executed = false;
        this.helper = helper;
    }
   

    [Fact]
    public async Task SubscriptionFired()
    {
        var services = new ServiceCollection();
        services.AddLogging(x => x.AddXUnit(this.helper));
        services.AddShinyMediator();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var tcs = new TaskCompletionSource();
        mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        await mediator.Publish(new TestEvent());
        tcs.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }


    [Fact]
    public async Task VariantHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging(x => x.AddXUnit(this.helper));
        services.AddShinyMediator();
        services.AddLogging();
        services.AddSingletonAsImplementedInterfaces<CatchAllEventHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        
        await mediator.Publish(new TestTestEvent());
        CatchAllEventHandler.Executed.ShouldBeTrue();
    }    
}

public class TestEvent : IEvent
{
    public int Delay { get; set; }
}
public class Test1EventHandler : IEventHandler<TestEvent>
{
    public async Task Handle(TestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        if (@event.Delay > 0)
            await Task.Delay(@event.Delay);
    }
}
public class Test2EventHandler : IEventHandler<TestEvent>
{
    public async Task Handle(TestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        if (@event.Delay > 0)
            await Task.Delay(@event.Delay);
    }
}

public class TestTestEvent : TestEvent;
public class CatchAllEventHandler : IEventHandler<TestTestEvent>
{
    public static bool Executed { get; set; }
    public Task Handle(TestTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}