using Shiny.Mediator;


public interface IRequestMiddleware<in TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> Process(TRequest request, Func<Task<TResult>> next, CancellationToken cancellationToken);
}