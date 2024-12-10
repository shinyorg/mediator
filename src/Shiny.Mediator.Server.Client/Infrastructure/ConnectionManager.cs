using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public interface IConnectionManager
{
    IReadOnlyList<HubConnection> Connections { get; }

    Task<ServerResult> Send(
        object request,
        DateTimeOffset? scheduleTime = null,
        DateTimeOffset? timeToLive = null,
        CancellationToken token = default
    );
}


public class ConnectionManager : IConnectionManager
{
    readonly Dictionary<Uri, HubConnection> connections = new();
    readonly MediatorServerConfig options;
    readonly ILogger logger;

    public ConnectionManager(    
        MediatorServerConfig options,
        ILogger<IConnectionManager> logger
    )
    {
        this.options = options;
        this.logger = logger;
        
        options
            .GetUniqueUris()
            .ToList()
            .ForEach(this.CreateHubConn);

        this.Connections = this.connections.Values.ToList();
    }


    void CreateHubConn(Uri uri)
    {
        var conn = new HubConnectionBuilder()
            .WithUrl(uri.ToString())
            .WithServerTimeout(TimeSpan.FromSeconds(10))
            .WithStatefulReconnect()
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithAutomaticReconnect()
            .Build();

        this.connections.Add(uri, conn);
    }


    public IReadOnlyList<HubConnection> Connections { get; }

    public Task<ServerResult> Send(
        object request, 
        DateTimeOffset? scheduleTime = null, 
        DateTimeOffset? timeToLive = null, 
        CancellationToken token = default
    )
    {
        var type = request.GetType();
        var uri = this.options.GetUriForContract(type);
        if (uri == null)
            throw new InvalidOperationException("No connection for " + uri);

        if (!this.connections.TryGetValue(uri, out HubConnection conn))
            throw new InvalidOperationException("No connection for " + uri);

        var jobj = JsonSerializer.SerializeToNode(request)!.AsObject();
        var message = new ServerMessage(
            type.Namespace + "." + type.Name,
            jobj,
            null,
            null
        );
        return conn.InvokeAsync<ServerResult>("Send", message, token);
    }
}