namespace Shiny.Mediator;


public interface IRequestHandler { }

public interface IRequestHandler<TRequest> : IRequestHandler where TRequest : IRequest
{
    Task Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<TRequest, TResult> : IRequestHandler where TRequest : IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}


public interface IStreamRequestHandler<TRequest, TResult> : IRequestHandler where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}