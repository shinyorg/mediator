namespace Shiny.Mediator;


public delegate IAsyncEnumerator<TResult> StreamRequestDelegate<TResult>(); 
public interface IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    // TODO: I want to be able to pump the async enumerable from the middleware as well
    IAsyncEnumerator<TResult> Process(
        TRequest request, 
        StreamRequestDelegate<TResult> next,
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    );
}

// public delegate Task<TResult> RequestHandlerDelegate<TResult>();
// public interface IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
// {
//     Task<TResult> Process(
//         TRequest request, 
//         RequestHandlerDelegate<TResult> next, 
//         IRequestHandler<TRequest, TResult> requestHandler, 
//         CancellationToken cancellationToken
//     );
// }