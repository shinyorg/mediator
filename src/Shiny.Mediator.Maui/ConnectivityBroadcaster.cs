using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public class ConnectivityBroadcaster(
    ILogger<ConnectivityBroadcaster> logger,
    IMediator mediator,
    IInternetService internetService,
    IApplication application
) : IMauiInitializeService
{
    bool connected = false;
    
    public void Initialize(IServiceProvider services)
    {
        this.connected = internetService.IsAvailable;
        
        internetService.StateChanged += async (_, conn) => await this.FireMediator(conn).ConfigureAwait(false);
        
        var app = application as Application;
        if (app == null)
        {
            logger.LogWarning("Application {application} not supported", application.GetType());
        }
        else
        {
            // this may be too late for the initial page
            // app.DescendantAdded += (sender, args) =>
            // {
            //     if (args.Element is Page page)
            //     {
            //         // page.BindingContextChanged
            //     }
            // };

            app.PageAppearing += async (_, page) =>
            {
                logger.LogDebug("Firing PageAppearing ConnectivityChanged for pages");
                if (page is IEventHandler<ConnectivityChanged> handler1)
                {
                    logger.LogDebug("Firing PageAppearing for {pageType}", page.GetType());
                    await this.TryAsHandler(handler1).ConfigureAwait(false);
                }

                if (page.BindingContext is IEventHandler<ConnectivityChanged> handler2)
                {
                    logger.LogDebug("Firing PageAppearing for {bindingContextType}", page.BindingContext.GetType());
                    await this.TryAsHandler(handler2).ConfigureAwait(false);
                }
            };
        }
    }
    

    async Task TryAsHandler(IEventHandler<ConnectivityChanged> handler)
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


    async Task FireMediator(bool conn)
    {
        try
        {
            logger.LogInformation("Firing Mediator Connection Changed to {conn}", conn);
            this.connected = conn;
                
            await mediator
                .Publish(new ConnectivityChanged(conn))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occured while connectivity Sprayer");
        }
    }
}