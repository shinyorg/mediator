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
        var handlerExec = new CommandHandlerDelegate(() =>
        {
            var postAction = context.Execution.OnHandlerExecute(context);
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}",
                commandHandler.GetType().FullName
            );
            
            return commandHandler
                .Handle(command, context, cancellationToken)
                .ContinueWith(_ => postAction.Invoke());
        });

        var middlewares = context.BypassMiddlewareEnabled ? [] : services.GetServices<ICommandMiddleware<TCommand>>();
        await middlewares
            .Reverse()
            .Aggregate(
                handlerExec, 
                (next, middleware) => () =>
                {
                    var postAction = context.Execution.OnMiddlewareExecute(context, middleware);
                    
                    logger.LogDebug(
                        "Executing request middleware {MiddlewareType}",
                        middleware.GetType().FullName
                    );

                    return middleware
                        .Process(
                            context,
                            next,
                            cancellationToken
                        )
                        .ContinueWith(_ => postAction.Invoke());
                }
            )
            .Invoke()
            .ConfigureAwait(false);
    }
}