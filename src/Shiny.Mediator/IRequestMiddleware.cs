using Shiny.Mediator;


public delegate Task<TResult> RequestHandlerDelegate<TResult>();
public interface IRequestMiddleware<TRequest, TResult>
{
    Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler, 
        CancellationToken cancellationToken
    );
}