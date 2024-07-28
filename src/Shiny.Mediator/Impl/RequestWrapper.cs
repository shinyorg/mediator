using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Impl;


class RequestWrapper<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        var logger = services.GetRequiredService<ILogger<TRequest>>();
        var handlerExec = new RequestHandlerDelegate<TResult>(() =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType} for {RequestType}", 
                requestHandler.GetType().FullName, 
                request.GetType().FullName
            );
            return requestHandler.Handle(request, cancellationToken);
        });

        var result = await RequestExecutor
            .Execute(services, request, logger, requestHandler, handlerExec, cancellationToken)
            .ConfigureAwait(false);
        
        return result;
    }
}