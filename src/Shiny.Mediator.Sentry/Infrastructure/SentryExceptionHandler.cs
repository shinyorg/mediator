namespace Shiny.Mediator.Infrastructure;

public class SentryExceptionHandler(IHub? hub = null) : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        hub?.CaptureException(exception);
        return Task.FromResult(false);
    }
}