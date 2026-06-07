using Microsoft.Extensions.Logging;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// <see cref="ICommandScheduler"/> backed by TickerQ. Serializes the deferred command and persists it as a
/// one-time TickerQ time ticker, so it survives process restarts and can run across a distributed cluster.
/// TickerQ must be configured separately (<c>services.AddTickerQ(...)</c> with an operational store and
/// <c>app.UseTickerQ()</c>).
/// </summary>
public class TickerQCommandScheduler(
    ITimeTickerManager<TimeTickerEntity> tickerManager,
    Shiny.ISerializer serializer,
    ILogger<TickerQCommandScheduler> logger
) : ICommandScheduler
{
    /// <inheritdoc/>
    public async Task Schedule(
        IMediatorContext context,
        DateTimeOffset dueAt,
        Func<IMediatorContext, CancellationToken, Task> dispatch,
        CancellationToken cancellationToken
    )
    {
        // dispatch is intentionally unused: TickerQ persists the command across restarts, so a runtime closure
        // cannot survive. ScheduledCommandRunner rehydrates and dispatches by runtime type when the ticker fires.
        _ = dispatch;
        var command = context.Message;
        var commandType = command.GetType();

        var payload = new ScheduledCommandPayload
        {
            CommandType = commandType.AssemblyQualifiedName!,
            // Runtime-typed serialize: the static type here is ICommand and a generic Serialize<T>(T) would
            // bind T to ICommand/object and emit "{}". Dispatch by Type via the extension so we serialize
            // against the concrete command shape.
            CommandJson = serializer.Serialize(command, commandType)
        };

        logger.LogInformation(
            "Scheduling command '{CommandType}' to run at {DueAt}",
            commandType.FullName,
            dueAt
        );

        await tickerManager
            .AddAsync(
                new TimeTickerEntity
                {
                    Function = ScheduledCommandRunner.FunctionName,
                    ExecutionTime = dueAt.UtcDateTime,
                    Request = TickerHelper.CreateTickerRequest(payload)
                },
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
