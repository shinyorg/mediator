using System.ComponentModel.DataAnnotations;

namespace Shiny.Mediator.Middleware;


[MiddlewareOrder(2)]
public class DataAnnotationsCommandMiddleware<TCommand> : AbstractValidationCommandMiddleware<TCommand> where TCommand : ICommand
{
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