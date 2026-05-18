using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Shiny.Mediator;


/// <summary>
/// Convenience helpers built on top of <see cref="IMediator"/>, including event awaiting,
/// event streaming, and tuple unwrapping for async-enumerable request results.
/// </summary>
public static class MediatorExtensions
{
    extension(IMediator mediator)
    {
        /// <summary>
        /// Subscribes to events of <typeparamref name="T"/> and returns when the first matching event arrives.
        /// The subscription is removed before returning.
        /// </summary>
        /// <param name="filter">Optional predicate; only events matching it complete the task.</param>
        /// <param name="cancellationToken"></param>
        public async Task<T> WaitForSingleEvent<T>(Func<T, bool>? filter = null,
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
        /// Subscribes to events of <typeparamref name="T"/> and yields each published event until the
        /// <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="filter">Optional predicate; only events matching it are yielded.</param>
        /// <param name="cancellationToken"></param>
        public async IAsyncEnumerable<T> EventStream<T>(Func<T, bool>? filter = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) where T : IEvent
        {
            var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });

            using var sub = mediator.Subscribe<T>((ev, ctx, ct) =>
            {
                if (filter?.Invoke(ev) ?? true)
                    channel.Writer.TryWrite(ev);

                return Task.CompletedTask;
            });
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                var item = await channel.Reader.ReadAsync(cancellationToken);
                yield return item;
            }
        }
    }


    // TODO: mediatorcontext does not have a subscribe
    // /// <summary>
    // /// Wait for event handler to fire
    // /// </summary>
    // /// <param name="mediator"></param>
    // /// <param name="filter">Allows you to filter the event instead of completing</param>
    // /// <param name="cancellationToken"></param>
    // /// <typeparam name="T"></typeparam>
    // /// <returns></returns>
    // public static async Task<T> WaitForSingleEvent<T>(
    //     this IMediatorContext context,
    //     Func<T, bool>? filter = null,
    //     CancellationToken cancellationToken = default
    // ) where T : IEvent
    // {
    //     var tcs = new TaskCompletionSource<T>();
    //     await using var u1 = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
    //     using var u2 = mediator.Subscribe<T>((ev, ctx, ct) =>
    //     {
    //         if (filter?.Invoke(ev) ?? true)
    //             tcs.TrySetResult(ev);
    //         return Task.CompletedTask;
    //     });
    //
    //     return await tcs.Task.ConfigureAwait(false);
    // }

    /// <summary>
    /// Strips the <see cref="IMediatorContext"/> from a mediator stream-request result, leaving only the
    /// result values. Useful when projecting the stream to an ASP.NET endpoint or other consumer that
    /// expects a plain <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public static async IAsyncEnumerable<T> UnwrapMediatorAsyncEnumerable<T>(this IAsyncEnumerable<(IMediatorContext Context, T Result)> source)
    {
        await foreach (var item in source)
            yield return item.Result;
    }


    /// <summary>
    /// Strips the <see cref="IMediatorContext"/> from a configured-cancellable mediator stream-request result,
    /// leaving only the result values.
    /// </summary>
    public static async IAsyncEnumerable<T> UnwrapMediatorAsyncEnumerable<T>(this ConfiguredCancelableAsyncEnumerable<(IMediatorContext Context, T Result)> source)
    {
        await foreach (var item in source)
            yield return item.Result;
    }
}
