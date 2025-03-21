namespace Shiny.Mediator;


public interface IStreamRequestHandler<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> Handle(
        TRequest request, 
        IMediatorContext context, 
        CancellationToken cancellationToken
    );
}