namespace Shiny.Mediator.Middleware;


public class MainTheadEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public async Task Process(
        EventExecutionContext<TEvent> context, 
        EventHandlerDelegate next
    )
    {
        var attr = context.EventHandler.GetHandlerHandleMethodAttribute<TEvent, MainThreadAttribute>();

        if (attr == null)
        {
            await next().ConfigureAwait(false);
        }
        else
        {
            var tcs = new TaskCompletionSource();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await next().ConfigureAwait(false);
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            await tcs.Task.ConfigureAwait(false);
        }
    }
}