using Shiny.Mediator;

public delegate Task<TResult> RequestHandlerDelegate<TResult>();
public interface IRequestMiddleware<TRequest, TResult>
{
    Task<TResult> Process(
        RequestContext<TRequest> context, 
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    );
}