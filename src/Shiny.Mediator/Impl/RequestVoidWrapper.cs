using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;


class RequestVoidWrapper<TRequest> where TRequest : IRequest
{
    public async Task Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IRequestHandler<TRequest>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var handlerExec = new RequestHandlerDelegate<Unit>(async () =>
        {
            await requestHandler.Handle(request, cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        });
        
        await RequestExecutor
            .Execute(services, request, requestHandler, handlerExec, cancellationToken)
            .ConfigureAwait(false);
    }
}