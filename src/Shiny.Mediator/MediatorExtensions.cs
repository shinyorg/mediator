using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    /// <param name="mediator"></param>
    extension(IMediator mediator)
    {
        /// <summary>
        /// Wait for event handler to fire
        /// </summary>
        /// <param name="filter">Allows you to filter the event instead of completing</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// Continues to wait for event handler responses
        /// </summary>
        /// <param name="filter">Allows you to filter the event before streaming it</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async IAsyncEnumerable<T> EventStream<T>(Func<T, bool>? filter = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) where T : IEvent
        {
            var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
        
            var tcs = new TaskCompletionSource<T>();
            await using var u1 = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            using var u2 = mediator.Subscribe<T>((ev, ctx, ct) =>
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
    /// This method unwraps the mediator context from an async enumerable result - useful for outgoing aspnet endpoints
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> UnwrapMediatorAsyncEnumerable<T>(this ConfiguredCancelableAsyncEnumerable<(IMediatorContext Context, T Result)> source)
    {
        await foreach (var item in source)
            yield return item.Result;
    }
}