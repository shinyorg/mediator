namespace Sample.Api.Handlers;


public class ScheduledGreeting : IScheduledCommand
{
    public DateTimeOffset DueAt { get; set; }
    public string Message { get; set; } = "";
}


[MediatorScoped]
public class ScheduledGreetingHandler(ILogger<ScheduledGreetingHandler> logger) : ICommandHandler<ScheduledGreeting>
{
    public Task Handle(ScheduledGreeting command, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Scheduled greeting fired at {Now}: {Message}", DateTimeOffset.UtcNow, command.Message);
        return Task.CompletedTask;
    }
}
