using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteBackgroundService(
    ILogger<RemoteBackgroundService> logger,
    IMediator mediator,
    MediatorServerConfig config,
    IServiceCollection services,
    IConnectionManager connectionManager
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // await connectionManager.Start();

        foreach (var service in services)
        {
            if (service.ImplementationType != null)
            {
                // TODO: must exclude all shiny internal handlers (obviously & especially RemoteRequestHandler)
                // TODO: must match to APP specific handler in scope
                // what about generic implementors - we also only want remote services here
                var requestContract = service.ImplementationType.GetServerRequestContract();
                var eventContract = service.ImplementationType.GetServerEventContract();
                
                // if (requestContract != null)
                //     yield return requestContract;
                //
                // if (eventContract != null)
                //     yield return eventContract;
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