using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class CommandSchedulingTests(ITestOutputHelper output)
{
    [Fact(Skip = "TODO")]
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

public class MySchedule : IScheduledCommand
{
    public DateTimeOffset DueAt { get; set; }
}

public class MyScheduleCommandHandler : ICommandHandler<MySchedule>
{
    public static TaskCompletionSource? Waiter { get; set; }
    public static bool Received { get; set; }
    public async Task Handle(MySchedule command, CommandContext<MySchedule> context, CancellationToken cancellationToken)
    {
        Waiter?.SetResult();
    }
}