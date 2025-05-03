using System.Diagnostics;

namespace Shiny.Mediator.Infrastructure;


//https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
public class ActivityTraceExecutionMiddleware : IExecutionMiddleware
{
    static readonly ActivitySource activitySource = new("Shiny.Mediator");

    // TODO: do I need a call for beginning the whole thing?
    public Action Start(IMediatorContext context)
    {
        // TODO: bypass things
        var activity = activitySource.StartActivity("Shiny.Mediator");
        activity?.SetTag("operation_id", context.Id);
        return () => activity?.Dispose();
    }

    public Action OnMiddlewareExecute(IMediatorContext context, object middleware)
    {
        
        var handlerName = context.MessageHandler!.GetType().FullName!;
        var middleName = middleware.GetType().FullName!;
        
        var activity = activitySource.StartActivity(middleName)!;
        
        // TODO: tag with current request and 
        
        foreach (var header in context.Headers)
            activity.SetTag(header.Key, header.Value);

        return () =>
        {
            activity.Dispose();
        };
    }

    public Action OnHandlerExecute(IMediatorContext context)
    {
        return () =>
        {

        };
    }
}