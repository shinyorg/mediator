using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Shiny.Mediator.Middleware;

public class DataAnnotationsRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler,
        CancellationToken cancellationToken
    )
    {
        var values = new Dictionary<string, List<string>>();
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results
        );

        foreach (var result in results!)
        {
            foreach (var member in result.MemberNames)
            {
                if (!values.ContainsKey(member))
                    values.Add(member, new List<string>());
        
                values[member].Add(result.ErrorMessage!);
            }
        }

        if (results.Count == 0)
        {
            var finalResult = await next().ConfigureAwait(false);
            
        }
        
        var validationResults = values
            .Select(x => new ValidateResult(x.Key, x.Value))
            .ToList();

        return null;
    }
}


// throw new ValidationException (string PropertyName, string ErrorMessage);
public interface IValidationRequest : IRequest<IDictionary<string, IList<string>>>
{
    
}

public record ValidateResult(string PropertyName, IList<string> Errors);
