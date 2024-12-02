using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Server.Client1;
using Sample.Server.Contracts;
using Shiny.Mediator;

Console.WriteLine("Client 1 Starting Up");

Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => logging.AddConsole())
    .ConfigureServices(services =>
    {
        var hubUri = new Uri("http://localhost:1999/mediator");
        services.AddShinyMediator(cfg => cfg.AddRemoteBus(y => y
            .Map<ChatEvent>(hubUri)
            .Map<TwoRequest>(hubUri)
        ));
        services.AddDiscoveredMediatorHandlersFromSample_Server_Client1();
    })
    .Build()
    .Run();