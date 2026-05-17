using System.ComponentModel.DataAnnotations;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Command-validation middleware that runs <see cref="System.ComponentModel.DataAnnotations"/> attributes
/// (e.g. <c>[Required]</c>, <c>[Range]</c>) against the incoming command. Registered by <c>AddDataAnnotations</c>.
/// </summary>
[MiddlewareOrder(2)]
public class DataAnnotationsCommandMiddleware<TCommand> : AbstractValidationCommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <inheritdoc/>
    protected override Task Validate(
        TCommand command, 
        Dictionary<string, List<string>> populate, 
        CancellationToken cancellationToken
    )
    {
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(
            command!,
            new ValidationContext(command!),
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