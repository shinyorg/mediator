namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseShinyFramework(
                new DryIocContainerExtension(),
                prism => prism.CreateWindow("NavigationPage/MainPage"),
                new(ErrorAlertType.FullError)
            )
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddDebug();
#endif
        builder.Services.AddShinyMediator<MauiEventCollector>();
        builder.Services.AddSingletonAsImplementedInterfaces<SingletonEventHandler>();
        builder.Services.AddSingletonAsImplementedInterfaces<SingletonRequestHandler>();
        builder.Services.RegisterForNavigation<MainPage, MainViewModel>();

        return builder.Build();
    }
}