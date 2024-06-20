namespace Shiny.Mediator;


public delegate IAsyncEnumerable<TResult> StreamRequestHandlerDelegate<TResult>(); 
public interface IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> Process(
        TRequest request, 
        StreamRequestHandlerDelegate<TResult> next,
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    );
}