using System.Runtime.CompilerServices;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    /// <summary>
    /// Wait for event handler to fire
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<T> WaitForEvent<T>(
        this IMediator mediator, 
        CancellationToken cancellationToken = default
    ) where T : IEvent
    {
        var tcs = new TaskCompletionSource<T>();
        await using var u1 = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        using var u2 = mediator.Subscribe<T>((ev, ctx, ct) =>
        {
            tcs.TrySetResult(ev);
            return Task.CompletedTask;
        });

        return await tcs.Task.ConfigureAwait(false);
    }


    /// <summary>
    /// Continues to wait for event handler responses
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> EventStream<T>(
        this IMediator mediator, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) where T : IEvent
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var e = await mediator.WaitForEvent<T>(cancellationToken);
            yield return e;
        }
    }
}