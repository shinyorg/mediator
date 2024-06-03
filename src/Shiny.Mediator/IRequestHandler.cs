namespace Shiny.Mediator;

public interface IRequestHandler<TRequest> where TRequest : notnull, IRequest
{
    Task Handle(TRequest request, CancellationToken cancellationToken);
}


public interface IRequestHandler<TRequest, TResult> where TRequest : notnull, IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}