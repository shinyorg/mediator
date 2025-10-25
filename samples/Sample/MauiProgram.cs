using System.Reflection;
using Microsoft.Extensions.Configuration;

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
        builder.Configuration.AddJsonStream(
            Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("Sample.appsettings.json")!
        );

        builder.AddShinyMediator(x => x
            .AddMediatorRegistry()
            .UseMaui()
            .UseBlazor()
            .PreventEventExceptions()
            .AddConnectivityBroadcaster()
            
            // Validation - you can only have both, but don't
            .AddDataAnnotations()
            // .AddFluentValidation() 
            
            .AddMauiHttpDecorator()
            .AddPrismSupport()
            
            .AddResiliencyMiddleware(builder.Configuration)
            // Cache - you can only have one
            .AddMauiPersistentCache()
            // .AddMemoryCaching(y =>
            // {
            //     y.ExpirationScanFrequency = TimeSpan.FromSeconds(5);
            // })
        );
        
        builder.Services.AddSingleton<AppSqliteConnection>();
        builder.Services.AddMauiBlazorWebView();

        builder.Services.RegisterForNavigation<TriggerPage, TriggerViewModel>();
        builder.Services.RegisterForNavigation<EventPage, EventViewModel>();
        builder.Services.RegisterForNavigation<BlazorPage, BlazorViewModel>();
        builder.Services.RegisterForNavigation<AnotherPage, AnotherViewModel>();
        
        var app = builder.Build();
        return app;
    }
}