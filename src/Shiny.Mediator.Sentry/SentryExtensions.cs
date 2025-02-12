namespace Shiny.Mediator;

public static class SentryExtensions
{
    public static ShinyConfigurator UseSentry(this ShinyConfigurator configurator)
    {
        // configurator.Logging
        return configurator;
    }
}