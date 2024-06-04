using Shiny.Mediator;


public delegate Task<TResult> RequestHandlerDelegate<TResult>();
public interface IRequestMiddleware<in TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> Process(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken);
}