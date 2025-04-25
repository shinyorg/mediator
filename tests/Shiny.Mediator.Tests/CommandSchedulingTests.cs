using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class CommandSchedulingTests(ITestOutputHelper output)
{
    readonly FakeTimeProvider fakeTime = new();

    // TODO: memory scheduler tests


    [Fact] public Task MediatorContext_DueDate_Null() => this.ScheduledDateRun(null, true);
    [Fact] public Task MediatorContext_DueDate_Past() => this.ScheduledDateRun(this.fakeTime.GetUtcNow().Subtract(TimeSpan.FromMinutes(1)), true);
    [Fact] public Task MediatorContext_DueDate_Future() => this.ScheduledDateRun(this.fakeTime.GetUtcNow().Add(TimeSpan.FromMinutes(1)), false);
    async Task ScheduledDateRun(DateTimeOffset? dueAt, bool expectedRun)
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        
        var schedulerLogger = TestHelpers.CreateLogger<ICommandScheduler>(output);
        var scheduler = new InMemoryCommandScheduler(schedulerLogger, this.fakeTime, sp);
        
        var middlewareLogger = TestHelpers.CreateLogger<ScheduledCommandMiddleware<MySchedule>>(output);
        var middleware = new ScheduledCommandMiddleware<MySchedule>(middlewareLogger, this.fakeTime, scheduler);

        var context = new MockMediatorContext
        {
            Message = new MySchedule(),
            MessageHandler = new MyScheduleCommandHandler()
        };

        var called = false;
        var del = new CommandHandlerDelegate(() =>
        {
             called = true;
             return Task.CompletedTask;
        });
        if (dueAt != null)
            context.SetCommandSchedule(dueAt.Value);
        
        await middleware.Process(context, del, CancellationToken.None);
        called.ShouldBe(expectedRun);
    }
    
    
    [Fact(Skip = "Broken test to come back to")]
    public async Task EndToEndTest()
    {
        MyScheduleCommandHandler.Received = false;
        
        var services = new ServiceCollection();
        var time = new FakeTimeProvider();

        services.AddSingleton<TimeProvider>(time);
        services.AddShinyMediator(x =>
        {
            x.AddInMemoryCommandScheduling();
        }, false);
        services.AddLogging(x => x.AddXUnit(output));
        services.AddSingletonAsImplementedInterfaces<MyScheduleCommandHandler>();
        MyScheduleCommandHandler.Waiter = new TaskCompletionSource();
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Send(new MySchedule
        {
            DueAt = time.GetUtcNow().AddMinutes(1)
        });
        MyScheduleCommandHandler.Waiter.Task.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromMinutes(1.5));

        await MyScheduleCommandHandler.Waiter.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }
}

file class MockCommandScheduler : ICommandScheduler
{
    public static bool ScheduleReply { get; set; }
    public Task Schedule(IMediatorContext context, DateTimeOffset dueAt, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}


file class MySchedule : IScheduledCommand
{
    public DateTimeOffset DueAt { get; set; }
}

file class MyScheduleCommandHandler : ICommandHandler<MySchedule>
{
    public static TaskCompletionSource? Waiter { get; set; }
    public static bool Received { get; set; }
    public async Task Handle(MySchedule command, IMediatorContext context, CancellationToken cancellationToken)
    {
        Waiter?.SetResult();
    }
}