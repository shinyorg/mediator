namespace Shiny.Mediator;

public interface IRequestHandler<in TRequest> where TRequest : notnull, IRequest
{
    Task Handle(TRequest request, CancellationToken cancellationToken);
}


public interface IRequestHandler<in TRequest, TResult> where TRequest : notnull, IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}