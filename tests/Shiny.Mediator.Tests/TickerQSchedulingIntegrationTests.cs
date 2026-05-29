using Microsoft.Extensions.Hosting;
using TickerQ.DependencyInjection;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class TickerQSchedulingIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ScheduledCommand_FiresThroughTickerQ_AndReDispatches()
    {
        var signal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddXUnitLogging(output);
        builder.Services.AddSingleton(signal);
        builder.Services.AddShinyMediator(x => x.AddTickerQCommandScheduling(), false);
        builder.Services.AddSingleton<ICommandHandler<IntegrationCommand>, IntegrationCommandHandler>();
        builder.Services.AddTickerQ();

        using var host = builder.Build();
        host.UseTickerQ();
        await host.StartAsync();
        try
        {
            var mediator = host.Services.GetRequiredService<IMediator>();
            await mediator.Send(new IntegrationCommand
            {
                DueAt = DateTimeOffset.UtcNow.AddSeconds(2),
                Value = 99
            });

            // a future-dated command must be deferred, not executed inline
            signal.Task.IsCompleted.ShouldBeFalse();

            var value = await signal.Task.WaitAsync(TimeSpan.FromSeconds(30));
            value.ShouldBe(99);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}


class IntegrationCommand : IScheduledCommand
{
    public DateTimeOffset DueAt { get; set; }
    public int Value { get; set; }
}


file class IntegrationCommandHandler(TaskCompletionSource<int> signal) : ICommandHandler<IntegrationCommand>
{
    public Task Handle(IntegrationCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        signal.TrySetResult(command.Value);
        return Task.CompletedTask;
    }
}
