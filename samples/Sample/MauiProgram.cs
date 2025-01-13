using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Polly;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UsePrism(
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
                )
            );

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddDebug();
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
        builder.Configuration.AddJsonStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("Sample.appsettings.json")!);
        
        builder.Services.AddShinyMediator(x => x
            .UseMaui()
            .UseBlazor()
            
            .AddOfflineAvailabilityMiddleware()
            .AddPersistentCache()
            .AddUserErrorNotificationsRequestMiddleware()
            .AddPerformanceLoggingMiddleware()
            .AddTimerRefreshStreamMiddleware()
            .AddEventExceptionHandlingMiddleware()
            .AddDataAnnotations()
            
            .AddHttpClient()
            .AddMauiHttpDecorator()
            .AddPrismSupport()
            
            // .AddFluentValidation() // don't add both
            .AddResiliencyMiddleware(builder.Configuration)
            .AddMemoryCaching(y =>
            {
                y.ExpirationScanFrequency = TimeSpan.FromSeconds(5);
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