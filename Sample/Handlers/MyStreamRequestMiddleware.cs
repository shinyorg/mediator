namespace Sample.Handlers;

public class MyStreamRequestMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerator<TResult> Process(
        TRequest request, 
        StreamRequestDelegate<TResult> next, 
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    )
    {
        return next();
    }
}