using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server;


public static class MediatorExtensions
{
    public static WebApplicationBuilder AddMediatorServer(this WebApplicationBuilder builder)
    {
        // TODO: add hub, data store, timer bg service?
        builder.Services.AddSingleton<IDataStore, MemoryDataStore>();
        builder.Services.AddHostedService<MediatorBackgroundService>();
        
        return builder;
    }


    public static WebApplication UseMediatorServer(this WebApplication app)
    {
        app.MapHub<MediatorHub>("/mediator");
        return app;
    }
}