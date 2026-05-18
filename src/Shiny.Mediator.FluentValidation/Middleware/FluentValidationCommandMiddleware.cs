using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.FluentValidation.Middleware;


/// <summary>
/// Command middleware that resolves an <see cref="IValidator{T}"/> for the incoming command from DI
/// and reports any validation failures through the mediator's standard validation flow.
/// </summary>
public class FluentValidationCommandMiddleware<TCommand>(IServiceProvider services) : AbstractValidationCommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <inheritdoc/>
    protected override async Task Validate(TCommand command, Dictionary<string, List<string>> populate, CancellationToken cancellationToken)
    {
        var validator = services.GetService<IValidator<TCommand>>();
        if (validator != null)
        {
            var result = await validator
                .ValidateAsync(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsValid)
            {
                foreach (var e in result.Errors)
                {
                    AddError(e.PropertyName, e.ErrorMessage, populate);
                }
            }
        }
    }
}
