namespace Shiny.Mediator.Infrastructure;


public class SentryRequestMiddleware<TRequest, TResult>(Func<IHub> getHub) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(RequestContext<TRequest> context, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}