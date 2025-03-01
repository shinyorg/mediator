namespace Shiny.Mediator;


public delegate IAsyncEnumerable<TResult> StreamRequestHandlerDelegate<TResult>(); 
public interface IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> Process(
        MediatorContext context, 
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    );
}