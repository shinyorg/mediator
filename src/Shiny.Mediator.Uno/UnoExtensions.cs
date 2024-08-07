using Shiny.Mediator.Uno.Infrastructure;

namespace Shiny.Mediator.Uno;


public static class UnoExtensions
{
    public static ShinyConfigurator AddUnoSupport(this ShinyConfigurator cfg)
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<UnoEventCollector>();
        return cfg;
    }
}