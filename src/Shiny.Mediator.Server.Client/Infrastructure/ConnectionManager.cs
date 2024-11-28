using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server.Client.Infrastructure;

public interface IConnectionManager
{
    Task Start();
    Task<ServerResult> Send(Type contractType, ServerMessage message, CancellationToken token = default);
}


public class ConnectionManager(
    MediatorHubOptions options,
    ILogger<IConnectionManager> logger
) : IConnectionManager
{
    readonly Dictionary<Uri, HubConnection> connections = new();
    public async Task Start()
    {
        var tasks = options
            .GetUniqueUris()
            .Select(this.CreateHubConn)
            .ToList();
        
        await Task.WhenAll(tasks);
        logger.LogInformation("Connection Manager Started - " + tasks.Count);
    }


    async Task CreateHubConn(Uri uri)
    {
        try
        {
            var conn = new HubConnectionBuilder()
                .WithUrl(uri.ToString())
                .WithServerTimeout(TimeSpan.FromSeconds(10))
                .WithStatefulReconnect()
                .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
                .WithAutomaticReconnect()
                .Build();

            logger.LogInformation("Starting Connection: " + uri);
            await conn.StartAsync(); // TODO: even if this errors, add it and try starting again later
            
            // TODO: get events & requests for uri
            // TODO: register & hooks
            this.connections.Add(uri, conn);
        }
        catch (Exception ex)
        {
            // TODO: do we try again later or fail out right away?
            logger.LogError(ex, "Failed to connect to hub");
        }
    }

    
    public Task<ServerResult> Send(Type contractType, ServerMessage message, CancellationToken token = default)
    {
        var uri = options.GetUriForContract(contractType);
        if (uri == null)
            throw new InvalidOperationException("");

        if (!this.connections.TryGetValue(uri, out HubConnection conn))
            throw new InvalidOperationException("");

        return conn.InvokeAsync<ServerResult>("Send", message, token);
    }
    
    
    
}