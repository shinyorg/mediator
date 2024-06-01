namespace Shiny.Mediator;

public interface IEventExceptionHandler
{
    Task Process(EventExceptionContext context);
}

public record EventExceptionContext(
    IEvent Event,
    Exception Exception,
    bool FireAndForget,  // you may wish to propagate the exception out if events are being await for completion 
    bool Handled
);