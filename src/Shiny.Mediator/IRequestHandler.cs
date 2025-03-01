namespace Shiny.Mediator;

public interface IRequestHandler { }


public interface IRequestHandler<TRequest, TResult> : IRequestHandler where TRequest : IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, MediatorContext context, CancellationToken cancellationToken);
}