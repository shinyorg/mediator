using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            cfg.Services.TryAddSingleton<IStorageService, StorageService>();
            cfg.Services.TryAddSingleton<IInternetService, InternetService>();
            cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        }
        return cfg;
    }
}