using System.ComponentModel.DataAnnotations;

namespace Shiny.Mediator.Middleware;

public class DataAnnotationsRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
{
    public Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler,
        CancellationToken cancellationToken
    )
    {
        // var values = new Dictionary<string, IList<string>>();
        // var results = new List<ValidationResult>();
        //
        // Validator.TryValidateObject(
        //     obj,
        //     new ValidationContext(obj),
        //     results
        // );
        //
        // foreach (var result in results)
        // {
        //     foreach (var member in result.MemberNames)
        //     {
        //         if (!values.ContainsKey(member))
        //             values.Add(member, new List<string>());
        //
        //         var errMsg = this.GetErrorMessage(obj, result);
        //         values[member].Add(errMsg);
        //     }
        // }
        // return values;
        throw new NotImplementedException();
    }
}


// throw new ValidationException (string PropertyName, string ErrorMessage);
public interface IValidationRequest : IRequest
{
    
}