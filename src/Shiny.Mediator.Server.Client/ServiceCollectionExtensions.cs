using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Server.Client.Infrastructure;

namespace Shiny.Mediator;


public static class ServiceCollectionExtensions
{
    public static ShinyConfigurator AddRemoteBus(this ShinyConfigurator configurator)
    {
        configurator.Services.AddHostedService<RemoteBackgroundService>();
        // TODO: namespace/contracts to a specific address?
        return configurator;
    }
}