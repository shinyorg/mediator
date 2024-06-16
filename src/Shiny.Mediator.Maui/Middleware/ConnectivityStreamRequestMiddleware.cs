namespace Shiny.Mediator.Middleware;

public class ConnectivityStreamRequestMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(TRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}