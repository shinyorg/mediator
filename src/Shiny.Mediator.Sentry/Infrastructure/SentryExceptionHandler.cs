namespace Shiny.Mediator.Infrastructure;

public class SentryExceptionHandler : IExceptionHandler
{
    public async Task<bool> Handle(object message, object handler, Exception exception)
    {
        SentrySdk.CaptureException(exception);
        return false;
    }
}