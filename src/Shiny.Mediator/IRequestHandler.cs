namespace Shiny.Mediator;


public interface IRequestHandler { }

public interface IRequestHandler<in TRequest> : IRequestHandler where TRequest : IRequest
{
    Task Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest, TResult> : IRequestHandler where TRequest : IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}