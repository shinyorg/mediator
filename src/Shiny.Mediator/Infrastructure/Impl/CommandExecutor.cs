using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class CommandExecutor : ICommandExecutor
{
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
        await middlewares
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
}