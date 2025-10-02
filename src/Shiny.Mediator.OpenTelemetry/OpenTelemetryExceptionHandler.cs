using System.Diagnostics;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryExceptionHandler : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        Activity.Current?.RecordException(exception);
        return Task.FromResult(false);
    }
}