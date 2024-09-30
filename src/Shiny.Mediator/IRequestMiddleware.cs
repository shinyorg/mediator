using Shiny.Mediator;

public delegate Task<TResult> RequestHandlerDelegate<TResult>();
public interface IRequestMiddleware<TRequest, TResult>
{
    Task<TResult> Process(
        ExecutionContext<TRequest> context, 
        RequestHandlerDelegate<TResult> next
    );
}