using Shiny.Mediator.FluentValidation.Middleware;

namespace Shiny.Mediator;


public static class FluentValidationExtensions
{
    /// <summary>
    /// Adds fluent validation to the mediator pipeline
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddFluentValidation(this ShinyConfigurator cfg)
        => cfg.AddOpenRequestMiddleware(typeof(FluentValidationRequestMiddleware<,>));
}