using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteBackgroundService(
    ILogger<RemoteBackgroundService> logger,
    IMediator mediator,
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
            await this.DoStart(conn, stoppingToken);
        }
    }


    async Task DoStart(HubConnection conn, CancellationToken stoppingToken)
    {
        // TODO: for all command handlers, register as owner
        // TODO: for all event handlers, register to receive
        
        await conn.StartAsync(stoppingToken);
        
        //https://learn.microsoft.com/sv-se/aspnet/core/signalr/hubs?view=aspnetcore-7.0#client-results
        // TODO: source generate these
        // conn.On<TRequest>("Type", msg =>
        // {
        //     try
        //     {
        //         // if request, there is a return
        //         // mediator.RequestWithContext(msg);
        //         // TODO: return server result
        //     }
        //     catch (Exception ex)
        //     {
        //         // TODO: return server result
        //     }
        // });
        
        // conn.On("Event")
        // TODO: command so we know how to respond
        // TODO: event
        // TODO: should we just always respond? 
        // TODO: likely yes so errors can be tracked

        // await conn.SendAsync("Register", null, stoppingToken);
    }
}