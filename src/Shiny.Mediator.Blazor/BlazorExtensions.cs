namespace Shiny.Mediator.Blazor;

public static class BlazorExtensions
{
    public static ShinyConfigurator UseBlazor(this ShinyConfigurator cfg)
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<BlazorEventCollector>();
        return cfg;
    }
}