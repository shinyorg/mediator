

namespace Shiny.Mediator.Tests;


public class EventHandlerTests
{
    public EventHandlerTests()
    {
        CatchAllEventHandler.Executed = false;
    }
   

    [Fact]
    public async Task Events_SubscriptionFired()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var tcs = new TaskCompletionSource();
        mediator.Subscribe<TestEvent>((_, _) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        await mediator.Publish(new TestEvent());
        tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
    }


    [Fact]
    public async Task Events_CatchAllHandler()
    {
        // TODO: test against event collectors as well
        var services = new ServiceCollection();
        services.AddShinyMediator();
        services.AddSingletonAsImplementedInterfaces<CatchAllEventHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        
        await mediator.Publish(new TestEvent());
        CatchAllEventHandler.Executed.Should().BeTrue();
    }    
}

public class TestEvent : IEvent
{
    public int Delay { get; set; }
}
public class Test1EventHandler : IEventHandler<TestEvent>
{
    public async Task Handle(TestEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Delay > 0)
            await Task.Delay(@event.Delay);
    }
}
public class Test2EventHandler : IEventHandler<TestEvent>
{
    public async Task Handle(TestEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Delay > 0)
            await Task.Delay(@event.Delay);
    }
}

public class CatchAllEventHandler : IEventHandler<IEvent>
{
    public static bool Executed { get; set; }
    public Task Handle(IEvent @event, CancellationToken cancellationToken)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}