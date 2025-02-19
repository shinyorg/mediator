using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public partial class Mediator
{
    public async Task<CommandContext<TCommand>> Send<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    ) where TCommand : ICommand
    {
        using var scope = services.CreateScope();
        var commandHandler = scope.ServiceProvider.GetService<ICommandHandler<TCommand>>();
        if (commandHandler == null)
            throw new InvalidOperationException("No command handler found for " + command.GetType().FullName);

        var context = new CommandContext<TCommand>(commandHandler, command);
        context.PopulateHeaders(headers);
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TCommand>>();
        var handlerExec = new CommandHandlerDelegate(async () =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                commandHandler.GetType().FullName 
            );
            await commandHandler
                .Handle(command, context, cancellationToken)
                .ConfigureAwait(false);
        });

        var middlewares = context.BypassMiddlewareEnabled() ? [] : scope.ServiceProvider.GetServices<ICommandMiddleware<TCommand>>();
        await middlewares
            .Reverse()
            .Aggregate(
                handlerExec, 
                (next, middleware) => () =>
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
            )
            .Invoke()
            .ConfigureAwait(false);

        return context;
    }
}