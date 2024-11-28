using System.Text.Json;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server.Client.Infrastructure;

public class RemoteRequestHandler<TRequest, TResult>(IConnectionManager connections) : IRequestHandler<TRequest, TResult>
    where TRequest : IServerRequest<TResult>
{

    public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var jobj = JsonSerializer.SerializeToNode(request)!.AsObject();
        var message = new ServerMessage(
            typeof(TRequest).Namespace + "." + typeof(TRequest).Name,
            jobj,
            null,
            null
        );
        var response = await connections.Send(typeof(TRequest), message, cancellationToken);
        var result = response.Payload.Deserialize<TResult>()!;
        return result;

        // TODO: make sure we're not intercepting a call BACK from signalr/hub
        // TODO: this has to be at the end of the pipeline since it intercepts
        // TODO: the handler won't exist locally so this will error right now unless I change this to a general handler?
    }
}