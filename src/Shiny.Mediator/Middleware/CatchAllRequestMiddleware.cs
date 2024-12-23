using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class CatchAllRequestMiddleware<TRequest, TResult>(ILogger<IMediator> logger) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(ExecutionContext<TRequest> context, RequestHandlerDelegate<TResult> next)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
           logger.LogError(ex, "An unhandled exception occurred"); 
        }
        return default(TResult);
    }
}