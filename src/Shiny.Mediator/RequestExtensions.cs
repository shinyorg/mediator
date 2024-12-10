namespace Shiny.Mediator;

public static class RequestExtensions
{
    /// <summary>
    /// Request data from a message
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<TResult> Request<TResult>(this IMediator mediator, IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var context = await mediator.RequestWithContext(request, cancellationToken).ConfigureAwait(false);
        return context.Result;
    }
    
    
    /// <summary>
    /// Requests a stream of data from a message
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static IAsyncEnumerable<TResult> Request<TResult>(this IMediator mediator, IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var context = mediator.RequestWithContext(request, cancellationToken);
        return context.Result;
    }
}