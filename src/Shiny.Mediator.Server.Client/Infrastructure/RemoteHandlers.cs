namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteRequestHandler<TRequest, TResult>(IConnectionManager connManager) 
    : IRequestHandler<TRequest, TResult> where TRequest : IServerRequest<TResult>
{
    public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var response = await connManager
            .Send(request, null, null, cancellationToken)
            .ConfigureAwait(false);
        
        response.ThrowIfFailed();
        return response.As<TResult>();
    }
}

public class RemoteEventHandler<TEvent>(IConnectionManager connManager) 
    : IEventHandler<TEvent> where TEvent : IServerEvent
{
    public async Task Handle(TEvent @event, EventContext context, CancellationToken cancellationToken)
    {
        var response = await connManager
            .Send(@event, null, null, cancellationToken)
            .ConfigureAwait(false);
        
        response.ThrowIfFailed();
    }
}