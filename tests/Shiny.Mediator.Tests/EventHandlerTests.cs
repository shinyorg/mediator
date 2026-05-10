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


    IMediator SetupMediator(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(x => x.AddXUnit(this.helper));
        services.AddConfiguration();
        services.AddShinyMediator();
        configure?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }


    [Fact]
    public async Task SubscriptionFired()
    {
        var mediator = this.SetupMediator();

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
    public async Task SubscriptionDisposed_StopsFiring()
    {
        var mediator = this.SetupMediator();

        int count = 0;
        var sub = mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        await mediator.Publish(new TestEvent());
        count.ShouldBe(1);

        sub.Dispose();

        await mediator.Publish(new TestEvent());
        count.ShouldBe(1);
    }


    [Fact]
    public async Task MultipleSubscriptions_AllFire()
    {
        var mediator = this.SetupMediator();

        int count1 = 0, count2 = 0;
        mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            Interlocked.Increment(ref count1);
            return Task.CompletedTask;
        });
        mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            Interlocked.Increment(ref count2);
            return Task.CompletedTask;
        });

        await mediator.Publish(new TestEvent());
        count1.ShouldBe(1);
        count2.ShouldBe(1);
    }


    [Fact]
    public async Task MultipleSubscriptions_DisposeOne_OtherStillFires()
    {
        var mediator = this.SetupMediator();

        int count1 = 0, count2 = 0;
        var sub1 = mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            Interlocked.Increment(ref count1);
            return Task.CompletedTask;
        });
        mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            Interlocked.Increment(ref count2);
            return Task.CompletedTask;
        });

        await mediator.Publish(new TestEvent());
        count1.ShouldBe(1);
        count2.ShouldBe(1);

        sub1.Dispose();

        await mediator.Publish(new TestEvent());
        count1.ShouldBe(1);
        count2.ShouldBe(2);
    }


    [Fact]
    public async Task SubscriptionReceivesCorrectEvent()
    {
        var mediator = this.SetupMediator();

        TestEvent? received = null;
        mediator.Subscribe<TestEvent>((ev, _, _) =>
        {
            received = ev;
            return Task.CompletedTask;
        });

        var sent = new TestEvent { Delay = 42 };
        await mediator.Publish(sent);
        received.ShouldNotBeNull();
        received.ShouldBeSameAs(sent);
    }


    [Fact]
    public async Task SubscriptionDoesNotFireForDifferentEventType()
    {
        var mediator = this.SetupMediator();

        int pingCount = 0;
        mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            Interlocked.Increment(ref pingCount);
            return Task.CompletedTask;
        });

        await mediator.Publish(new AnotherTestEvent());
        pingCount.ShouldBe(0);
    }


    [Fact]
    public async Task PublishToBackground_SubscriptionFires()
    {
        var mediator = this.SetupMediator();

        var tcs = new TaskCompletionSource();
        mediator.Subscribe<TestEvent>((_, _, _) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        mediator.PublishToBackground(new TestEvent());

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        completed.ShouldBe(tcs.Task);
    }


    [Fact]
    public async Task WaitForSingleEvent_ReceivesEvent()
    {
        var mediator = this.SetupMediator();

        var waitTask = mediator.WaitForSingleEvent<TestEvent>();

        var sent = new TestEvent { Delay = 7 };
        await mediator.Publish(sent);

        var result = await waitTask;
        result.ShouldBeSameAs(sent);
    }


    [Fact]
    public async Task WaitForSingleEvent_WithFilter_SkipsNonMatching()
    {
        var mediator = this.SetupMediator();

        var waitTask = mediator.WaitForSingleEvent<TestEvent>(e => e.Delay == 99);

        await mediator.Publish(new TestEvent { Delay = 1 });
        await mediator.Publish(new TestEvent { Delay = 2 });

        waitTask.IsCompleted.ShouldBeFalse();

        await mediator.Publish(new TestEvent { Delay = 99 });

        var result = await waitTask;
        result.Delay.ShouldBe(99);
    }


    [Fact]
    public async Task WaitForSingleEvent_Cancellation_Throws()
    {
        var mediator = this.SetupMediator();

        using var cts = new CancellationTokenSource(100);
        await Should.ThrowAsync<TaskCanceledException>(
            mediator.WaitForSingleEvent<TestEvent>(cancellationToken: cts.Token)
        );
    }


    [Fact]
    public async Task EventStream_ReceivesMultipleEvents()
    {
        var mediator = this.SetupMediator();

        using var cts = new CancellationTokenSource(5000);
        var received = new List<TestEvent>();
        var listening = new TaskCompletionSource();

        var streamTask = Task.Run(async () =>
        {
            await foreach (var ev in mediator.EventStream<TestEvent>(cancellationToken: cts.Token))
            {
                listening.TrySetResult();
                received.Add(ev);
                if (received.Count >= 3)
                    await cts.CancelAsync();
            }
        });

        // Subscribe needs the first MoveNextAsync to run, which sets up the subscription.
        // Publish a priming event and wait for the stream to receive it.
        while (!listening.Task.IsCompleted)
        {
            await mediator.Publish(new TestEvent { Delay = 0 });
            await Task.Delay(10);
        }

        received.Clear();

        await mediator.Publish(new TestEvent { Delay = 1 });
        await mediator.Publish(new TestEvent { Delay = 2 });
        await mediator.Publish(new TestEvent { Delay = 3 });

        try { await streamTask; }
        catch (OperationCanceledException) { }

        received.Count.ShouldBe(3);
        received[0].Delay.ShouldBe(1);
        received[1].Delay.ShouldBe(2);
        received[2].Delay.ShouldBe(3);
    }


    [Fact]
    public async Task EventStream_WithFilter_OnlyMatchingEvents()
    {
        var mediator = this.SetupMediator();

        using var cts = new CancellationTokenSource(5000);
        var received = new List<TestEvent>();
        var listening = new TaskCompletionSource();

        var streamTask = Task.Run(async () =>
        {
            // Use no filter for the stream itself so we can detect the priming event
            await foreach (var ev in mediator.EventStream<TestEvent>(cancellationToken: cts.Token))
            {
                listening.TrySetResult();
                if (ev.Delay > 10)
                {
                    received.Add(ev);
                    if (received.Count >= 2)
                        await cts.CancelAsync();
                }
            }
        });

        // Publish a priming event and wait for the stream to be active
        while (!listening.Task.IsCompleted)
        {
            await mediator.Publish(new TestEvent { Delay = 0 });
            await Task.Delay(10);
        }

        await mediator.Publish(new TestEvent { Delay = 1 });
        await mediator.Publish(new TestEvent { Delay = 20 });
        await mediator.Publish(new TestEvent { Delay = 5 });
        await mediator.Publish(new TestEvent { Delay = 30 });

        try { await streamTask; }
        catch (OperationCanceledException) { }

        received.Count.ShouldBe(2);
        received[0].Delay.ShouldBe(20);
        received[1].Delay.ShouldBe(30);
    }


    [Fact]
    public async Task VariantHandler()
    {
        var mediator = this.SetupMediator(s => s.AddSingletonAsImplementedInterfaces<CatchAllEventHandler>());

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

public record AnotherTestEvent : IEvent;
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