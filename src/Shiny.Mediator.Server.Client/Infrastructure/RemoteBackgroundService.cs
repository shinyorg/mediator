using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteBackgroundService(IServiceProvider services, IOptions<MediatorRemoteOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        await conn.SendAsync("Register", new
        {

        });

        // TODO: for all command handlers, register as owner
        // TODO: for all event handlers, register to receive
    }
}