using Polly;
using Polly.Retry;
using Sample.Handlers;
using Shiny.Mediator.Resilience;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UseShinyFramework(
                new DryIocContainerExtension(),
                prism => prism.CreateWindow(nav => nav
                    .CreateBuilder()
                    .AddTabbedSegment(tabs => tabs
                        .CreateTab(tab => tab
                            .AddNavigationPage()
                            .AddSegment(nameof(TriggerPage))
                        )
                        .CreateTab(tab => tab
                            .AddNavigationPage()
                            .AddSegment(nameof(EventPage))
                        )
                        .CreateTab(tab => tab
                            .AddNavigationPage()
                            .AddSegment(nameof(BlazorPage))
                        )
                    )
                ),
                new(ErrorAlertType.FullError)
            );

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddDebug();
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
        builder.Services.AddShinyMediator(x => x
            .UseMaui()
            .UseBlazor()
            .AddTimerRefreshStreamMiddleware()
            // .AddReplayStreamMiddleware()
            .AddUserNotificationExceptionMiddleware(new UserExceptionRequestMiddlewareConfig
            {
                ErrorConfirm = "OK",
                ErrorTitle = "OOOPS",
                ErrorMessage = "You did something wrong",
                ShowFullException = false
            })
            // .AddResiliencyMiddleware(
            //     ("Test", builder =>
            //     {
            //         // builder.AddRetry(new RetryStrategyOptions());
            //         builder.AddTimeout(TimeSpan.FromSeconds(2.0));
            //     })
            // )
            .AddMemoryCaching(x =>
            {
                x.ExpirationScanFrequency = TimeSpan.FromSeconds(5);
            })
        );
        builder.Services.AddDiscoveredMediatorHandlersFromSample();
        
        builder.Services.AddSingleton<AppSqliteConnection>();
        builder.Services.AddMauiBlazorWebView();

        builder.Services.RegisterForNavigation<TriggerPage, TriggerViewModel>();
        builder.Services.RegisterForNavigation<EventPage, EventViewModel>();
        builder.Services.RegisterForNavigation<BlazorPage, BlazorViewModel>();
        builder.Services.RegisterForNavigation<AnotherPage, AnotherViewModel>();
        
        return builder.Build();
    }
}