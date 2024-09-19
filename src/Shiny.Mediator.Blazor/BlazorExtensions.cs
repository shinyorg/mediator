using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Blazor.Infrastructure;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public static class BlazorExtensions
{
    public static ShinyConfigurator UseBlazor(this ShinyConfigurator cfg)
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<BlazorEventCollector>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Browser")))
        {
            cfg.Services.AddSingleton<IStorageService, StorageService>();
            cfg.Services.AddSingleton<IInternetService, InternetService>();
        }
        return cfg;
    }
}