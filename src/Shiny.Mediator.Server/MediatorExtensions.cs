using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Server;


public static class MediatorExtensions
{
    public static IServiceCollection AddMediatorServer(this IServiceCollection services)
    {
        // TODO: add hub, data store, timer bg service?
        return services;
    }
}