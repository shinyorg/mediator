using System.Runtime.CompilerServices;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    /// <summary>
    /// Wait for event handler to fire
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="filter">Allows you to filter the event instead of completing</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<T> WaitForEvent<T>(
        this IMediator mediator, 
        Func<T, bool>? filter = null,
        CancellationToken cancellationToken = default
    ) where T : IEvent
    {
        var tcs = new TaskCompletionSource<T>();
        await using var u1 = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        using var u2 = mediator.Subscribe<T>((ev, ctx, ct) =>
        {
            if (filter?.Invoke(ev) ?? true)
                tcs.TrySetResult(ev);
            return Task.CompletedTask;
        });

        return await tcs.Task.ConfigureAwait(false);
    }


    /// <summary>
    /// Continues to wait for event handler responses
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="filter">Allows you to filter the event before streaming it</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> EventStream<T>(
        this IMediator mediator, 
        Func<T, bool>? filter = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) where T : IEvent
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var e = await mediator.WaitForEvent<T>(filter, cancellationToken);
            yield return e;
        }
    }
}