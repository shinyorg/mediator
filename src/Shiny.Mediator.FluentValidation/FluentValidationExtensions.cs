using System.Reflection;
using FluentValidation;
using Shiny.Mediator.FluentValidation.Middleware;

namespace Shiny.Mediator;


/// <summary>
/// Registration extensions for plugging FluentValidation into the Shiny Mediator pipeline.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Registers FluentValidation-backed request and command middleware. Any <c>IValidator&lt;T&gt;</c>
    /// resolved from DI is invoked before its corresponding handler; failures are aggregated and
    /// surfaced through the standard mediator validation pipeline.
    /// </summary>
    /// <param name="cfg">The mediator builder to wire into.</param>
    /// <param name="validatorAssemblies">
    /// Optional assemblies to scan for <see cref="IValidator{T}"/> implementations via
    /// <c>AddValidatorsFromAssemblies</c>. Scanning uses reflection and may not be AOT compatible;
    /// validators must still be present in the DI container to be picked up by middleware.
    /// </param>
    /// <returns>The same builder for chaining.</returns>
    public static ShinyMediatorBuilder AddFluentValidation(this ShinyMediatorBuilder cfg, params IEnumerable<Assembly> validatorAssemblies)
    {
        cfg.AddOpenRequestMiddleware(typeof(FluentValidationRequestMiddleware<,>));
        cfg.AddOpenCommandMiddleware(typeof(FluentValidationCommandMiddleware<>));
        if (validatorAssemblies.Any())
            cfg.Services.AddValidatorsFromAssemblies(validatorAssemblies);

        return cfg;
    }
}
