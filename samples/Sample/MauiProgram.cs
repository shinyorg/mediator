using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>();

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

        builder.Services.AddSingleton<IDialogService, DialogService>();

        // Shell resolves pages from DI, and each page takes its view model
        builder.Services.AddTransient<TriggerPage>();
        builder.Services.AddTransient<TriggerViewModel>();
        builder.Services.AddTransient<EventPage>();
        builder.Services.AddTransient<EventViewModel>();
        builder.Services.AddTransient<BlazorPage>();
        builder.Services.AddTransient<BlazorViewModel>();
        builder.Services.AddTransient<AnotherPage>();
        builder.Services.AddTransient<AnotherViewModel>();
        
        var app = builder.Build();
        return app;
    }
}