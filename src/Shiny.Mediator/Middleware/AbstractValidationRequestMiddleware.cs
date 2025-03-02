using System.Reflection;

namespace Shiny.Mediator.Middleware;


public abstract class AbstractValidationRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        if (context.Message!.GetType().GetCustomAttribute<ValidateAttribute>() == null)
            return await next();

        var values = new Dictionary<string, List<string>>();
        await this.Validate((TRequest)context.Message, values, cancellationToken).ConfigureAwait(false);
        
        if (values.Count == 0)
        {
            var finalResult = await next().ConfigureAwait(false);
            return finalResult;
        }

        var dict = (IReadOnlyDictionary<string, IReadOnlyList<string>>)values.ToDictionary(
            x => x.Key,
            x => (IReadOnlyList<string>)x.Value
        );
        var validationResults = new ValidateResult(dict);
        
        if (typeof(TResult) != typeof(ValidateResult))
            throw new ValidateException(validationResults);

        return (TResult)(object)validationResults;
    }


    protected static void AddError(string key, string error, Dictionary<string, List<string>> populate)
    {
        if (!populate.ContainsKey(key))
            populate.Add(key, new List<string>());

        populate[key].Add(error);
    }
    
    
    protected abstract Task Validate(TRequest request, Dictionary<string, List<string>> populate, CancellationToken cancellationToken);
}
