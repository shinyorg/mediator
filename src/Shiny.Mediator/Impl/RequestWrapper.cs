using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;


class RequestWrapper<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var handlerExec = new RequestHandlerDelegate<TResult>(()
            => requestHandler.Handle(request, cancellationToken));

        var result = await RequestExecutor
            .Execute(services, request, requestHandler, handlerExec, cancellationToken)
            .ConfigureAwait(false);
        
        return result;
    }
}