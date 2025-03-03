using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public class ConnectivityBroadcaster(
    ILogger<ConnectivityBroadcaster> logger,
    IMediator mediator,
    IInternetService internetService
) : IMauiInitializeService
{
    bool connected = false;
    
    public async void Initialize(IServiceProvider services)
    {
        this.connected = internetService.IsAvailable;
        
        internetService.StateChanged += async (_, conn) =>
        {
            try
            {
                this.connected = conn;
                
                await mediator
                    .Publish(new ConnectivityChanged(conn))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occured while connectivity Sprayer");
            }
        };

        var app = await this.WaitForApp().ConfigureAwait(false);
        if (app != null)
            app.PageAppearing += async (_, page) => await this.TryPageBinding(page);
    }


    async Task TryPageBinding(Page page)
    {
        if (page is IEventHandler<ConnectivityChanged> handler1)
            await this.TryBinding(handler1).ConfigureAwait(false);
                
        if (page.BindingContext is IEventHandler<ConnectivityChanged> handler2)
            await this.TryBinding(handler2).ConfigureAwait(false);
    }


    async Task TryBinding(IEventHandler<ConnectivityChanged> handler)
    {
        try
        {
            var e = new ConnectivityChanged(this.connected);
            await handler
                .Handle(
                    e,
                    null!,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rebroadcast connectivity");
        }
    }

    
    async Task<Application?> WaitForApp()
    {
        logger.LogDebug("Waiting for MAUI app");
        var count = 0;
        Application? app = null;
        while (count < 5 && app == null)
        {
            app = Application.Current;
            if (app == null)
            {
                await Task.Delay(500).ConfigureAwait(false);
                count++;
            }
        }

        if (app == null)
            logger.LogDebug("MAUI app was not found");

        return app;
    }
}

public record ConnectivityChanged(bool Connected) : IEvent;
public interface IConnectivityEventHandler : IEventHandler<ConnectivityChanged>;