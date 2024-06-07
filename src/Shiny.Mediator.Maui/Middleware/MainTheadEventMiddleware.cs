using System.ComponentModel;
using System.Reflection;

namespace Shiny.Mediator.Middleware;


[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class MainThreadAttribute : Attribute {}

public class MainTheadEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public async Task Process(IEvent @event, EventHandlerDelegate next, IEventHandler<TEvent> eventHandler, CancellationToken cancellationToken)
    {
        // TODO: could pull an attribute off the method of the eventhandler instance method
        var hasAttribute = eventHandler
            .GetType()
            .GetMethod("Handle")!
            .GetCustomAttribute<MainThreadAttribute>() != null;
        
        if (hasAttribute)
        {
            var tcs = new TaskCompletionSource();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await next();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            await tcs.Task.ConfigureAwait(false);
        }
        else
        {
            await next().ConfigureAwait(false);
        }
    }
}