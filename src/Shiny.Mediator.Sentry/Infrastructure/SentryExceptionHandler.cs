namespace Shiny.Mediator.Infrastructure;

public class SentryExceptionHandler : IExceptionHandler
{
    public async Task<bool> Handle(MediatorContext context, Exception exception)
    {
        SentrySdk.CaptureException(exception);
        return false;
    }
}