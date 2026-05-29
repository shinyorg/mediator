using System.Reflection;
using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Base;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// TickerQ function that rehydrates a scheduled command persisted by <see cref="TickerQCommandScheduler"/> and
/// re-dispatches it through the mediator when its time ticker fires. Registered with TickerQ automatically via the
/// source-generated module initializer in this assembly - no manual wiring is required.
/// </summary>
public class ScheduledCommandRunner(
    IMediator mediator,
    ISerializerService serializer,
    ILogger<ScheduledCommandRunner> logger
)
{
    static readonly MethodInfo SendMethod = typeof(IMediator).GetMethod(nameof(IMediator.Send))!;

    /// <summary>
    /// The TickerQ function name used for every mediator scheduled command.
    /// </summary>
    public const string FunctionName = "Shiny.Mediator.ScheduledCommand";


    /// <summary>
    /// Invoked by TickerQ when a scheduled command is due. Deserializes the command and sends it through the mediator.
    /// </summary>
    [TickerFunction(FunctionName)]
    public async Task Execute(
        TickerFunctionContext<ScheduledCommandPayload> context,
        CancellationToken cancellationToken
    )
    {
        var payload = context.Request;
        if (payload == null)
            throw new InvalidOperationException("Scheduled command ticker fired with no request payload");

        var commandType = Type.GetType(payload.CommandType);
        if (commandType == null)
            throw new InvalidOperationException($"Could not resolve scheduled command type '{payload.CommandType}'");

        var command = (ICommand)serializer.Deserialize(payload.CommandJson, commandType);
        logger.LogInformation("Executing scheduled command '{CommandType}'", commandType.FullName);

        Action<IMediatorContext> configure = ctx =>
        {
            // the schedule has already been evaluated, so run the handler directly (matches InMemoryCommandScheduler)
            ctx.BypassMiddlewareEnabled = true;
            ctx.BypassExceptionHandlingEnabled = true;
        };

        // dispatch with the runtime command type as TCommand so the mediator resolves ICommandHandler<TActual>;
        // a statically-typed Send(command) binds TCommand to ICommand and finds no handler
        var send = SendMethod.MakeGenericMethod(commandType);
        try
        {
            await ((Task)send.Invoke(mediator, [command, cancellationToken, configure])!).ConfigureAwait(false);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }
}
