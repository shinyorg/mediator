namespace Shiny.Mediator;

public class RequestContext(IRequestHandler handler) : AbstractMediatorContext
{
    public IRequestHandler Handler => handler;
}

public class RequestContext<TRequest>(
    TRequest request, 
    IRequestHandler handler 
) : RequestContext(handler)
{
    public TRequest Request => request;
}