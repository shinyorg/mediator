using System.Reflection;
using FluentValidation;
using Shiny.Mediator.FluentValidation.Middleware;

namespace Shiny.Mediator;


public static class FluentValidationExtensions
{
    /// <summary>
    /// Adds fluent validation to the mediator pipeline
    /// </summary>
    /// <param name="cfg"></param>
    /// <param name="validatorAssemblies">
    /// The assemblies to scan for validators - this may break AOT and is here for convenience
    /// NOTE: validators must exist on the dependency injection container to be picked up by Shiny Mediator
    /// </param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddFluentValidation(this ShinyMediatorBuilder cfg, params IEnumerable<Assembly> validatorAssemblies)
    {
        cfg.AddOpenRequestMiddleware(typeof(FluentValidationRequestMiddleware<,>));
        cfg.AddOpenCommandMiddleware(typeof(FluentValidationCommandMiddleware<>));
        if (validatorAssemblies.Any())
            cfg.Services.AddValidatorsFromAssemblies(validatorAssemblies);

        return cfg;
    }
}