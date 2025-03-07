using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.FluentValidation.Middleware;


public class FluentValidationRequestMiddleware<TRequest, TResult>(IServiceProvider services) : AbstractValidationRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
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