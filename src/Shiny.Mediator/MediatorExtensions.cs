using Microsoft.Extensions.Logging;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    /// <summary>
    /// Fire & Forget task pattern
    /// </summary>
    /// <param name="task"></param>
    /// <param name="onError"></param>
    public static void RunInBackground(this Task task, Action<Exception> onError)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                onError(x.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    /// <summary>
    /// Fire & Forget task pattern that logs errors
    /// </summary>
    /// <param name="task"></param>
    /// <param name="errorLogger"></param>
    public static void RunInBackground(this Task task, ILogger errorLogger)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                errorLogger.LogError(x.Exception, "Fire & Forget trapped error");
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    /// <summary>
    /// Request data from a message
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<(IMediatorContext Context, TResult Result)> RequestWithContext<TResult>(
        this IMediator mediator, 
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        IMediatorContext capture = null!;
        var result = await mediator
            .Request(request, cancellationToken, ctx =>
            {
                capture = ctx;
                configure?.Invoke(ctx);
            })
            .ConfigureAwait(false);
        
        return (capture, result);
    }
    
    /// <summary>
    /// Requests a stream of data from a message
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static (IMediatorContext Context, IAsyncEnumerable<TResult> Result) RequestWithContext<TResult>(
        this IMediator mediator, 
        IStreamRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        IMediatorContext capture = null!;
        var result = mediator.Request(request, cancellationToken, ctx =>
        {
            capture = ctx;
            configure?.Invoke(capture);
        });
        return (capture, result);
    }
}