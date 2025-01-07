namespace Shiny.Mediator;


public interface IStreamRequestHandler<TRequest, TResult> : IRequestHandler where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}