using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;

public partial class Mediator
{
    public async Task Send<TCommand>(TCommand request, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        using var scope = services.CreateScope();
        var requestHandler = scope.ServiceProvider.GetService<ICommandHandler<TCommand>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TCommand>>();
        var handlerExec = new CommandHandlerDelegate(async () =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                requestHandler.GetType().FullName 
            );
            await requestHandler.Handle(request, cancellationToken).ConfigureAwait(false);
        });
        //
        // var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
        // var middlewares = scope.ServiceProvider.GetServices<IRequestMiddleware<TRequest, Unit>>();
        // await middlewares
        //     .Reverse()
        //     .Aggregate(
        //         handlerExec, 
        //         (next, middleware) => () =>
        //         {
        //             logger.LogDebug(
        //                 "Executing request middleware {MiddlewareType}",
        //                 middleware.GetType().FullName
        //             );
        //             
        //             return middleware.Process(context, next);
        //         }
        //     )
        //     .Invoke()
        //     .ConfigureAwait(false);
        //
    }
}


// public interface IRequestResultWrapper<TResult>
// {
//     Task<ExecutionResult<TResult>> Handle();
// }
// public class CommandWrapper<TRequest, TResult>(
//     IServiceProvider scope, 
//     TRequest request,
//     CancellationToken cancellationToken
// ) : IRequestResultWrapper<TResult> where TRequest : IRequest<TResult>
// {
//     public async Task<ExecutionResult<TResult>> Handle()
//     {
//         var requestHandler = scope.GetService<IRequestHandler<TRequest, TResult>>();
//         if (requestHandler == null)
//             throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
//         
//         var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
//         var middlewares = scope.GetServices<IRequestMiddleware<TRequest, TResult>>();
//         var logger = scope.GetRequiredService<ILogger<TRequest>>();
//         
//         var handlerExec = new RequestHandlerDelegate<TResult>(() =>
//         {
//             logger.LogDebug(
//                 "Executing request handler {RequestHandlerType}", 
//                 requestHandler.GetType().FullName 
//             );
//             return requestHandler.Handle(context.Request, context.CancellationToken);
//         });
//         
//         var result = await middlewares
//             .Reverse()
//             .Aggregate(
//                 handlerExec, 
//                 (next, middleware) => () =>
//                 {
//                     logger.LogDebug(
//                         "Executing request middleware {MiddlewareType}",
//                         middleware.GetType().FullName
//                     );
//                     
//                     return middleware.Process(context, next);
//                 }
//             )
//             .Invoke()
//             .ConfigureAwait(false);
//         
//         return new ExecutionResult<TResult>(context, result);
//     }
// }