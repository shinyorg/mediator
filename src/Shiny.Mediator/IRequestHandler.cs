namespace Shiny.Mediator;

public interface IRequestHandler<TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, IMediatorContext context, CancellationToken cancellationToken);
}