using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


/// <summary>
/// Default in-process <see cref="ICommandExecutor"/>. Resolves the registered <see cref="ICommandHandler{TCommand}"/>
/// and runs it through the ordered middleware chain.
/// </summary>
public class LocalCommandExecutor : ICommandExecutor
{
    /// <inheritdoc/>
    public async Task Send<TCommand>(
        IMediatorContext context,
        TCommand command,
        CancellationToken cancellationToken
    ) where TCommand : ICommand
    {
        var services = context.ServiceScope!.ServiceProvider;
        var commandHandler = services.GetService<ICommandHandler<TCommand>>();
        
        if (commandHandler == null)
            throw new InvalidOperationException("No command handler found for " + command.GetType().FullName);

        context.MessageHandler = commandHandler;

        var logger = services.GetRequiredService<ILogger<TCommand>>();
        var handlerExec = new CommandHandlerDelegate(async () =>
        {
            using (var handlerActivity = context.StartActivity("ExecutingHandler"))
            {
                logger.LogDebug(
                    "Executing request handler {RequestHandlerType}",
                    commandHandler.GetType().FullName
                );
                await commandHandler
                    .Handle(command, context, cancellationToken)
                    .ConfigureAwait(false);
            }
        });

        var middlewares = context.BypassMiddlewareEnabled ? [] : services.GetServices<ICommandMiddleware<TCommand>>();
        await MiddlewareOrderResolver.OrderMiddleware(middlewares)
            .Reverse()
            .Aggregate(
                handlerExec, 
                (next, middleware) => () =>
                {
                    using (var handlerActivity = context.StartActivity("ExecutingMiddleware"))
                    {
                        logger.LogDebug(
                            "Executing request middleware {MiddlewareType}",
                            middleware.GetType().FullName
                        );

                        return middleware.Process(
                            context,
                            next,
                            cancellationToken
                        );
                    }
                }
            )
            .Invoke()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool CanSend<TCommand>(TCommand command) where TCommand : ICommand => true;
}