using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteBackgroundService(IConnectionManager connectionManager) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await connectionManager.Start();

        // TODO: hook each hub




        // await conn.SendAsync("Register", new ClusterRegistration(
        //     "TODO",
        //     handledRequests.ToArray(),
        //     subEventTypes.ToArray()
        // ));

        // TODO: for all command handlers, register as owner
        // TODO: for all event handlers, register to receive
    }
}