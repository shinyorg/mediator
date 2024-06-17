namespace Shiny.Mediator;


public interface IStreamRequestHandler<in TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}