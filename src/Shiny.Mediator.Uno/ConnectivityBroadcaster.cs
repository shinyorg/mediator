using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny.Mediator;


public class ConnectivityBroadcaster(
    ILogger<ConnectivityBroadcaster> logger,
    IMediator mediator,
    IInternetService internetService
) : IServiceInitialize
{
    public void Initialize()
    {
        internetService.StateChanged += async (_, connected) =>
        {
            try
            {
                await mediator.Publish(new ConnectivityChanged(connected));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occured while connectivity Sprayer");
            }
        };
    }
}

public record ConnectivityChanged(bool Connected) : IEvent;