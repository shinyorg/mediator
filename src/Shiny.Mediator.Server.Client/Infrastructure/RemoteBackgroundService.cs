using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteBackgroundService(IServiceCollection services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO: configuration
        // TODO: connection per distinct URI
        var conn = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000")
            .WithServerTimeout(TimeSpan.FromSeconds(10))
            .WithStatefulReconnect()
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithAutomaticReconnect()
            .Build();
        
        await conn.StartAsync(stoppingToken);
        conn.On("Command", async () =>
        {

        });
        conn.On("Event", async () =>
        {

        });

        
        


        
        // await conn.SendAsync("Register", new ClusterRegistration(
        //     "TODO",
        //     handledRequests.ToArray(),
        //     subEventTypes.ToArray()
        // ));

        // TODO: for all command handlers, register as owner
        // TODO: for all event handlers, register to receive
    }
}