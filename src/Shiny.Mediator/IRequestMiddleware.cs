using Shiny.Mediator;

public delegate Task<TResult> RequestHandlerDelegate<TResult>();
public interface IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> Process(
        IMediatorContext context, 
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    );
}