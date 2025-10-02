using System.Diagnostics;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryExceptionHandler : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            currentActivity.RecordException(exception);
            currentActivity.SetStatus(ActivityStatusCode.Error, exception.Message);
            
            currentActivity.SetTag("exception.type", exception.GetType().FullName);
            currentActivity.SetTag("exception.message", exception.Message);
            
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                currentActivity.SetTag("exception.stacktrace", exception.StackTrace);
            }
        }
        
        return Task.FromResult(false);
    }
}