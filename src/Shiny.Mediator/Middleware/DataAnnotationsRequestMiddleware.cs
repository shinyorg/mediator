using System.ComponentModel.DataAnnotations;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Request-validation middleware that runs <see cref="System.ComponentModel.DataAnnotations"/> attributes
/// (e.g. <c>[Required]</c>, <c>[Range]</c>) against the incoming request. Registered by <c>AddDataAnnotations</c>.
/// </summary>
[MiddlewareOrder(2)]
public class DataAnnotationsRequestMiddleware<TRequest, TResult> : AbstractValidationRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    /// <inheritdoc/>
    protected override Task Validate(
        TRequest request, 
        Dictionary<string, List<string>> populate, 
        CancellationToken cancellationToken
    )
    {
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(
            request!,
            new ValidationContext(request!),
            results
        );
        
        foreach (var result in results!)
        {
            foreach (var member in result.MemberNames)
            {
                AddError(member, result.ErrorMessage!, populate);
            }
        }

        return Task.CompletedTask;
    }
}