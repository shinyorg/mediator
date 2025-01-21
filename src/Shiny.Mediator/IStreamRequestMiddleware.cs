namespace Shiny.Mediator;


public delegate IAsyncEnumerable<TResult> StreamRequestHandlerDelegate<TResult>(); 
public interface IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> Process(
        RequestContext<TRequest> context, 
        StreamRequestHandlerDelegate<TResult> next
    );
}