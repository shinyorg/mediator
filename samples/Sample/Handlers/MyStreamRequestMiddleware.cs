namespace Sample.Handlers;

// [RegisterMiddleware]
public class MyStreamRequestMiddleware<TRequest, TResult>(ILogger<MyStreamRequestMiddleware<TRequest, TResult>> logger) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(
        RequestContext<TRequest> context, 
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation($"MyStreamRequestMiddleware called for {typeof(TRequest).FullName}");
        return next();
    }
}