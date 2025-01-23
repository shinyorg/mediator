using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class MainThreadCommandMiddleware<TCommand>(
    ILogger<MainThreadCommandMiddleware<TCommand>> logger
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public Task Process(
        CommandContext<TCommand> context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var attr = context.Handler.GetHandlerHandleMethodAttribute<TCommand, MainThreadAttribute>();
        if (attr == null)
            return next();

        logger.LogDebug("MainThread Enabled - {Request}", context.Command);
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
        return tcs.Task;
    }
}