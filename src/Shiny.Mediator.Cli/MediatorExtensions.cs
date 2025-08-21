using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;

namespace Shiny.Mediator;


public static class MediatorConsoleExtensions
{
    public static ShinyMediatorBuilder UseConsole(this ShinyMediatorBuilder builder)
    {
        var context = (CliCommandCollector?)builder
            .Services
            .FirstOrDefault(x => x
                .ImplementationInstance?.GetType() == typeof(CliCommandCollector)
            )?
            .ImplementationInstance;

        if (context == null)
        {
            builder.Services.AddSingleton(new CliCommandCollector());
            context = (CliCommandCollector)builder
                .Services
                .First(x => x.ImplementationInstance?.GetType() == typeof(CliCommandCollector))
                .ImplementationInstance!;
        }

        //context.Add(route, handlerType);
        // TODO: map up cli args
        builder.Services.TryAddSingleton<ICliMediatorRunner, CliMediatorRunner>();
        return builder;
    }
    
    
    public static Task RunMediation(this IHost host, string[] args)
        => host.Services.GetRequiredService<ICliMediatorRunner>().Execute(args);
}