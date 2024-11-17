namespace Shiny.Mediator.Server.Client.Infrastructure;

public class RemoteRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IServerRequest<TResult>
{
    public Task<TResult> Process(ExecutionContext<TRequest> context, RequestHandlerDelegate<TResult> next)
    {
        // TODO: make sure we're not intercepting a call BACK from signalr/hub
        // TODO: this has to be at the end of the pipeline since it intercepts
            // TODO: the handler won't exist locally so this will error right now unless I change this to a general handler?
        throw new NotImplementedException();
    }
}