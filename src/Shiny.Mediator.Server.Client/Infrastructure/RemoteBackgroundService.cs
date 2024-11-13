using Microsoft.Extensions.Hosting;

namespace Shiny.Mediator.Server.Client.Infrastructure;


public class RemoteBackgroundService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}