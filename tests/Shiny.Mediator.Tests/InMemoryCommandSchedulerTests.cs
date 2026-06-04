using Microsoft.Extensions.Time.Testing;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class InMemoryCommandSchedulerTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DispatchesCommand_OnceDue()
    {
        var time = new FakeTimeProvider();
        var signal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var sp = this.BuildProvider(time, services =>
        {
            services.AddSingleton(signal);
            services.AddSingleton<ICommandHandler<DeferredCommand>, DeferredCommandHandler>();
        });
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new DeferredCommand { Value = 7, DueAt = time.GetUtcNow().AddSeconds(30) });
        signal.Task.IsCompleted.ShouldBeFalse();

        time.Advance(TimeSpan.FromMinutes(1));

        var value = await signal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        value.ShouldBe(7);
    }


    [Fact]
    public async Task DoesNotDispatch_BeforeDue()
    {
        var time = new FakeTimeProvider();
        var signal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var sp = this.BuildProvider(time, services =>
        {
            services.AddSingleton(signal);
            services.AddSingleton<ICommandHandler<DeferredCommand>, DeferredCommandHandler>();
        });
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new DeferredCommand { Value = 1, DueAt = time.GetUtcNow().AddMinutes(5) });

        time.Advance(TimeSpan.FromMinutes(1)); // the poll fires, but the command is not due for another 4 minutes
        signal.Task.IsCompleted.ShouldBeFalse();
    }


    [Fact]
    public async Task DispatchesAllDueCommands()
    {
        var time = new FakeTimeProvider();
        var collector = new Collector(2);

        var sp = this.BuildProvider(time, services =>
        {
            services.AddSingleton(collector);
            services.AddSingleton<ICommandHandler<DeferredCommand>, CollectingHandler>();
        });
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new DeferredCommand { Value = 1, DueAt = time.GetUtcNow().AddSeconds(30) });
        await mediator.Send(new DeferredCommand { Value = 2, DueAt = time.GetUtcNow().AddSeconds(30) });

        time.Advance(TimeSpan.FromMinutes(1));

        await collector.Completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        collector.Values.OrderBy(x => x).ShouldBe([1, 2]);
    }


    [Fact]
    public async Task HandlerException_DoesNotPreventOtherCommands()
    {
        var time = new FakeTimeProvider();
        var signal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var sp = this.BuildProvider(time, services =>
        {
            services.AddSingleton(signal);
            services.AddSingleton<ICommandHandler<BoomCommand>, BoomCommandHandler>();
            services.AddSingleton<ICommandHandler<DeferredCommand>, DeferredCommandHandler>();
        });
        var mediator = sp.GetRequiredService<IMediator>();

        // the failing command is scheduled first; its exception must not stop the second from running
        await mediator.Send(new BoomCommand { DueAt = time.GetUtcNow().AddSeconds(30) });
        await mediator.Send(new DeferredCommand { Value = 42, DueAt = time.GetUtcNow().AddSeconds(30) });

        time.Advance(TimeSpan.FromMinutes(1));

        var value = await signal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        value.ShouldBe(42);
    }


    IServiceProvider BuildProvider(FakeTimeProvider time, Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddXUnitLogging(output);
        services.AddShinyMediator(x => x.AddInMemoryCommandScheduling(), false);
        configure(services);
        return services.BuildServiceProvider();
    }
}


class DeferredCommand : IScheduledCommand
{
    public DateTimeOffset DueAt { get; set; }
    public int Value { get; set; }
}


class BoomCommand : IScheduledCommand
{
    public DateTimeOffset DueAt { get; set; }
}


file class DeferredCommandHandler(TaskCompletionSource<int> signal) : ICommandHandler<DeferredCommand>
{
    public Task Handle(DeferredCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        signal.TrySetResult(command.Value);
        return Task.CompletedTask;
    }
}


file class BoomCommandHandler : ICommandHandler<BoomCommand>
{
    public Task Handle(BoomCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("boom");
}


file class CollectingHandler(Collector collector) : ICommandHandler<DeferredCommand>
{
    public Task Handle(DeferredCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        collector.Add(command.Value);
        return Task.CompletedTask;
    }
}


file class Collector(int expected)
{
    readonly List<int> values = new();
    readonly object gate = new();

    public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyList<int> Values
    {
        get
        {
            lock (this.gate)
                return this.values.ToArray();
        }
    }

    public void Add(int value)
    {
        lock (this.gate)
        {
            this.values.Add(value);
            if (this.values.Count >= expected)
                this.Completed.TrySetResult();
        }
    }
}
