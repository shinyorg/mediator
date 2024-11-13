namespace Shiny.Mediator.Server.Client.Infrastructure;

public class RemoteRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IServerRequest<TResult>
{
    public Task<TResult> Process(ExecutionContext<TRequest> context, RequestHandlerDelegate<TResult> next)
    {
        throw new NotImplementedException();
    }
}