using System.Collections.Concurrent;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;

namespace Shiny.Mediator.Tests;


public class MainThreadMiddlewareTests
{
    readonly FakeMainThreadDispatcher dispatcher = new();


    [Fact]
    public async Task Request_WithAttribute_RunsOnDispatcher()
    {
        var handler = new MainThreadRequestHandler();
        var request = new MainThreadRequest();
        var context = new MockMediatorContext { Message = request, MessageHandler = handler };
        var middleware = new MainThreadRequestMiddleware<MainThreadRequest, int>(this.dispatcher);

        var result = await middleware.Process(
            context,
            () => handler.Handle(request, context, CancellationToken.None),
            CancellationToken.None
        );

        result.ShouldBe(42);
        handler.RanOnMainThread.ShouldBeTrue();
        this.dispatcher.Invocations.ShouldBe(1);
    }


    [Fact]
    public async Task Request_WithoutAttribute_RunsInline()
    {
        var handler = new PlainRequestHandler();
        var request = new PlainRequest();
        var context = new MockMediatorContext { Message = request, MessageHandler = handler };
        var middleware = new MainThreadRequestMiddleware<PlainRequest, int>(this.dispatcher);

        var result = await middleware.Process(
            context,
            () => handler.Handle(request, context, CancellationToken.None),
            CancellationToken.None
        );

        result.ShouldBe(1);
        this.dispatcher.Invocations.ShouldBe(0);
    }


    [Fact]
    public async Task Command_WithAttribute_RunsOnDispatcher()
    {
        var handler = new MainThreadCommandHandler();
        var command = new MainThreadCommand();
        var context = new MockMediatorContext { Message = command, MessageHandler = handler };
        var middleware = new MainThreadCommandMiddleware<MainThreadCommand>(this.dispatcher);

        await middleware.Process(
            context,
            () => handler.Handle(command, context, CancellationToken.None),
            CancellationToken.None
        );

        handler.RanOnMainThread.ShouldBeTrue();
        this.dispatcher.Invocations.ShouldBe(1);
    }


    [Fact]
    public async Task Event_WithAttribute_RunsOnDispatcher_AndPropagatesException()
    {
        var handler = new MainThreadEventHandler { Throw = true };
        var @event = new MainThreadEvent();
        var context = new MockMediatorContext { Message = @event, MessageHandler = handler };
        var middleware = new MainTheadEventMiddleware<MainThreadEvent>(this.dispatcher);

        await Should.ThrowAsync<InvalidOperationException>(() => middleware.Process(
            context,
            () => handler.Handle(@event, context, CancellationToken.None),
            CancellationToken.None
        ));
        handler.RanOnMainThread.ShouldBeTrue();
    }


    [Fact]
    public async Task CancelledBeforeDispatch_SkipsHandler()
    {
        var handler = new MainThreadCommandHandler();
        var command = new MainThreadCommand();
        var context = new MockMediatorContext { Message = command, MessageHandler = handler };
        var middleware = new MainThreadCommandMiddleware<MainThreadCommand>(this.dispatcher);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<TaskCanceledException>(() => middleware.Process(
            context,
            () => handler.Handle(command, context, cts.Token),
            cts.Token
        ));
        handler.RanOnMainThread.ShouldBeFalse();
    }


    [Fact]
    public void AddMainThreadMiddleware_PreservesExistingDispatcher_AndRegistersOnce()
    {
        var services = new ServiceCollection();
        var existing = new FakeMainThreadDispatcher();
        services.AddSingleton<IMainThreadDispatcher>(existing);
        services.AddShinyMediator(cfg => cfg
            .AddMainThreadMiddleware<FakeMainThreadDispatcher>()
            .AddMainThreadMiddleware<FakeMainThreadDispatcher>()
        );
        services.Count(x => x.ImplementationType == typeof(MainThreadRequestMiddleware<,>)).ShouldBe(1);

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMainThreadDispatcher>().ShouldBeSameAs(existing);
    }
}


/// <summary>
/// Runs queued work on a dedicated thread, standing in for a UI thread.
/// </summary>
public class FakeMainThreadDispatcher : IMainThreadDispatcher
{
    readonly BlockingCollection<Action> queue = new();
    readonly Thread thread;
    int invocations;

    public FakeMainThreadDispatcher()
    {
        this.thread = new Thread(() =>
        {
            foreach (var action in this.queue.GetConsumingEnumerable())
                action();
        }) { IsBackground = true, Name = ThreadName };
        this.thread.Start();
    }

    const string ThreadName = "FakeMainThread";
    public static bool IsCurrentThread => Thread.CurrentThread.Name == ThreadName;
    public int Invocations => this.invocations;
    public bool IsMainThread => Thread.CurrentThread == this.thread;

    public void BeginInvokeOnMainThread(Action action)
    {
        Interlocked.Increment(ref this.invocations);
        this.queue.Add(action);
    }
}


public record MainThreadRequest : IRequest<int>;
public partial class MainThreadRequestHandler : IRequestHandler<MainThreadRequest, int>
{
    public bool RanOnMainThread { get; private set; }

    [MainThread]
    public Task<int> Handle(MainThreadRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        this.RanOnMainThread = FakeMainThreadDispatcher.IsCurrentThread;
        return Task.FromResult(42);
    }
}

public record PlainRequest : IRequest<int>;
public class PlainRequestHandler : IRequestHandler<PlainRequest, int>
{
    public Task<int> Handle(PlainRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(1);
}

public record MainThreadCommand : ICommand;
public partial class MainThreadCommandHandler : ICommandHandler<MainThreadCommand>
{
    public bool RanOnMainThread { get; private set; }

    [MainThread]
    public Task Handle(MainThreadCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        this.RanOnMainThread = FakeMainThreadDispatcher.IsCurrentThread;
        return Task.CompletedTask;
    }
}

public record MainThreadEvent : IEvent;
public partial class MainThreadEventHandler : IEventHandler<MainThreadEvent>
{
    public bool Throw { get; set; }
    public bool RanOnMainThread { get; private set; }

    [MainThread]
    public Task Handle(MainThreadEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        this.RanOnMainThread = FakeMainThreadDispatcher.IsCurrentThread;
        if (this.Throw)
            throw new InvalidOperationException("boom");

        return Task.CompletedTask;
    }
}
