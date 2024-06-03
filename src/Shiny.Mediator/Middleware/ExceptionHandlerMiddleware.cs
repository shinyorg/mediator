namespace Shiny.Mediator.Middleware;

public class ExceptionHandlerMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(TRequest request, Func<Task<TResult>> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
            
        }
        catch (Exception ex)
        {
            // TODO: log, trap?
            throw;
        }
    }
}