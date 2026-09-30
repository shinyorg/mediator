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
            results,
            validateAllProperties: true // otherwise only [Required] runs - [Range], [Url], [StringLength], etc. are skipped
        );
        
        foreach (var result in results)
        {
            // IValidatableObject / class-level attributes may not name a member - key those under "" (object-level,
            // as ASP.NET ModelState does) rather than dropping them, which let the contract pass validation
            var anyMember = false;
            foreach (var member in result.MemberNames)
            {
                anyMember = true;
                AddError(member, result.ErrorMessage!, populate);
            }
            if (!anyMember)
                AddError(String.Empty, result.ErrorMessage!, populate);
        }

        return Task.CompletedTask;
    }
}