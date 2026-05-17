using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.FluentValidation.Middleware;


/// <summary>
/// Request middleware that resolves an <see cref="IValidator{T}"/> for the incoming request from DI
/// and reports any validation failures through the mediator's standard validation flow.
/// </summary>
public class FluentValidationRequestMiddleware<TRequest, TResult>(IServiceProvider services) : AbstractValidationRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    /// <inheritdoc/>
    protected override async Task Validate(TRequest request, Dictionary<string, List<string>> populate, CancellationToken cancellationToken)
    {
        var validator = services.GetService<IValidator<TRequest>>();
        if (validator != null)
        {
            var result = await validator
                .ValidateAsync(request, cancellationToken)
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
