using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


class RequestWrapper<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<ExecutionResult<TResult>> Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var logger = services.GetRequiredService<ILogger<TRequest>>();
        var handlerExec = new RequestHandlerDelegate<TResult>(() =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                requestHandler.GetType().FullName 
            );
            return requestHandler.Handle(request, cancellationToken);
        });

        var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
        var result = await RequestExecutor
            .Execute(services, context, handlerExec)
            .ConfigureAwait(false);
        
        return new ExecutionResult<TResult>(context, result);
    }
}