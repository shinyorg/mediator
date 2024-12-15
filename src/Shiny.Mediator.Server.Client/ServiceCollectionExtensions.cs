using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Server.Client;
using Shiny.Mediator.Server.Client.Infrastructure;

namespace Shiny.Mediator;


public static class ServiceCollectionExtensions
{
    public static ShinyConfigurator AddRemote(this ShinyConfigurator configurator,
        Action<MediatorServerConfig> configure)
    {
        var cfg = new MediatorServerConfig();
        configure.Invoke(cfg);
        configurator.Services.AddSingleton(cfg);

        configurator.Services.AddHostedService<RemoteBackgroundService>();
        configurator.Services.TryAddSingleton<IConnectionManager, ConnectionManager>();
        configurator.Services.AddSingleton(typeof(IRequestHandler<,>), typeof(RemoteRequestHandler<,>));
        configurator.Services.AddSingleton(typeof(IEventHandler<>), typeof(RemoteEventHandler<>));
        configurator.Services.AddSingleton(configurator.Services);


        // TODO: source generate
        // connection.On<TRequest>("Type Name", arg => { mediator.Request(arg); }
        // connection.On<TEvent>("Type Name", arg => { mediator.Publish(arg); }

        // TODO: create a mediator options hub with contract to uri maps
        // TODO: contract collector scans all registered request & event handlers existings HERE
        // TODO: if a contract type handler is found without a configuration, we warn or error depending on config
        // TODO: we should only be looking at server bound specific handlers!!
        // TODO: log/consolewrite all handler to URIs

        // TODO: background task turns on and hooks REQUEST against remote hub server for each unique URL and request handler
        // TODO: background task registers with HUB and ensures hub likes requests
        // TODO: hub will begin pushing any scheduled requests (that haven't expired)
        // and events (that haven't expired) IF the app is still interested in those events
        // TODO: background task hooks all EVENT handlers against remote hub server for each unique URL
        // TODO: background task executes mediator for each incoming request
        // TODO: if mediator errors, we report back to hub
        // TODO: we should report back to the server with execution time?

        return configurator;
    }
}