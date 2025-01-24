using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class CommandSchedulingTests(ITestOutputHelper output)
{
    [Fact]
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
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Send(new MySchedule
        {
            DueAt = DateTimeOffset.UtcNow.AddMinutes(1)
        });
        MyScheduleCommandHandler.Received.ShouldBeFalse();
        time.Advance(TimeSpan.FromMinutes(1.5));

        await Task.Delay(1000); // let the thing execute now
        MyScheduleCommandHandler.Received.ShouldBeTrue();
    }
}

public class MySchedule : IScheduledCommand
{
    public DateTimeOffset DueAt { get; set; }
}

public class MyScheduleCommandHandler : ICommandHandler<MySchedule>
{
    public static bool Received { get; set; }
    public async Task Handle(MySchedule command, CommandContext<MySchedule> context, CancellationToken cancellationToken)
    {
        Received = true;
    }
}