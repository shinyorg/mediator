using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server;


public static class MediatorExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static WebApplicationBuilder AddMediatorServer(
        this WebApplicationBuilder builder,
        Action<HubOptions>? configure = null
    )
    {
        // TODO: add hub, data store, timer bg service?
        builder.Services.AddSignalR(opts =>
        {
            configure?.Invoke(opts);
        });
        builder.Services.TryAddSingleton<IDataStore, MemoryDataStore>();
        builder.Services.AddHostedService<MediatorBackgroundService>();
        
        return builder;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="hubName"></param>
    /// <returns></returns>
    public static WebApplication UseMediatorServer(this WebApplication app, string hubName = "/mediator")
    {
        app.MapHub<MediatorHub>(hubName);
        return app;
    }
}