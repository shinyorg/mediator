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

    
//     #if __IOS__
//             events.AddiOS(lifecycle =>
//             {
//                 lifecycle.FinishedLaunching((application, launchOptions) =>
//                 {
//                     // A bit of hackery here, because we can't mock UIKit.UIApplication in tests.
//                     var platformApplication = application != null!
//                         ? application.Delegate as IPlatformApplication
//                         : launchOptions["application"] as IPlatformApplication;
//
//                     platformApplication?.HandleMauiEvents();
//                     return true;
//                 });
//                 lifecycle.WillTerminate(application =>
//                 {
//                     if (application == null!)
//                     {
//                         return;
//                     }
//
//                     var platformApplication = application.Delegate as IPlatformApplication;
//                     platformApplication?.HandleMauiEvents(bind: false);
//
//                     //According to https://developer.apple.com/documentation/uikit/uiapplicationdelegate/1623111-applicationwillterminate#discussion
//                     //WillTerminate is called: in situations where the app is running in the background (not suspended) and the system needs to terminate it for some reason.
//                     SentryMauiEventProcessor.InForeground = false;
//                 });
//
//                 lifecycle.OnActivated(application => SentryMauiEventProcessor.InForeground = true);
//
//                 lifecycle.DidEnterBackground(application => SentryMauiEventProcessor.InForeground = false);
//                 lifecycle.OnResignActivation(application => SentryMauiEventProcessor.InForeground = false);
//             });
// #elif ANDROID
//             events.AddAndroid(lifecycle =>
//             {
//                 lifecycle.OnApplicationCreating(application => (application as IPlatformApplication)?.HandleMauiEvents());
//                 lifecycle.OnDestroy(application => (application as IPlatformApplication)?.HandleMauiEvents(bind: false));
//
//                 lifecycle.OnResume(activity => SentryMauiEventProcessor.InForeground = true);
//                 lifecycle.OnStart(activity => SentryMauiEventProcessor.InForeground = true);
//
//                 lifecycle.OnStop(activity => SentryMauiEventProcessor.InForeground = false);
//                 lifecycle.OnPause(activity => SentryMauiEventProcessor.InForeground = false);
//             });
// #elif WINDOWS
//             events.AddWindows(lifecycle =>
//             {
//                 lifecycle.OnLaunching((application, _) => (application as IPlatformApplication)?.HandleMauiEvents());
//                 lifecycle.OnClosed((application, _) => (application as IPlatformApplication)?.HandleMauiEvents(bind: false));
//             });
// #endif

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