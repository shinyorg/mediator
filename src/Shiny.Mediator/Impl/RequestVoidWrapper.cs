using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Impl;


class RequestVoidWrapper<TRequest> where TRequest : IRequest
{
    public async Task Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IRequestHandler<TRequest>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var logger = services.GetRequiredService<ILogger<TRequest>>();
        var handlerExec = new RequestHandlerDelegate<Unit>(async () =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                requestHandler.GetType().FullName 
            );
            await requestHandler.Handle(request, cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        });

        var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
        await RequestExecutor
            .Execute(
                services, 
                context,
                handlerExec 
            )
            .ConfigureAwait(false);
    }
}