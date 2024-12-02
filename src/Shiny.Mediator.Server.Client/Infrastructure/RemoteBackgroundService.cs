using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteBackgroundService(
    ILogger<RemoteBackgroundService> logger,
    MediatorServerConfig config,
    IConnectionManager connectionManager,
    IContractCollector collector
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // await connectionManager.Start();

        var types = collector.Collect();

        // TODO: should I register each type individually since connection is a stream
        foreach (var type in types)
        {
            var contractUri = config.GetUriForContract(type);
            if (type.IsEvent())
            {
                
            }
            else if (type.IsRequest())
            {
                
            }
        }

        foreach (var conn in connectionManager.Connections)
        {
            // TODO: keep trying to reconnect
            // conn.On("Request", async _ =>)
            // conn.On("Event")
            // await conn.StartAsync(stoppingToken);
            // await conn.SendAsync("Register", null, stoppingToken);
        }
        
        // TODO: hook each hub
            // TODO: command so we know how to respond
            // TODO: event
                // TODO: should we just always respond? 
                // TODO: likely yes so errors can be tracked
        // TODO: register
        




        // await conn.SendAsync("Register", new ClusterRegistration(
        //     "TODO",
        //     handledRequests.ToArray(),
        //     subEventTypes.ToArray()
        // ));

        // TODO: for all command handlers, register as owner
        // TODO: for all event handlers, register to receive
    }
}