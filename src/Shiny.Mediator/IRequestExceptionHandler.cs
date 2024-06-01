namespace Shiny.Mediator;

public interface IRequestExceptionrHandler
{
    // should this be abortable or even async?
    Task Handle(RequestExceptionContext context, CancellationToken cancellationToken);
}

// what about events?
public class RequestExceptionContext
{
    public RequestExceptionContext(object request, Exception exception)
    {
        this.Request = request;
        this.Exception = exception;
    }
    
    
    public object Request { get; }
    public Exception Exception { get; }
    public bool Handled { get; set; }
}