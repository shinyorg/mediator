using System.Reflection;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Base class for request-validation middleware. Skips when the request's contract type is not decorated with
/// <c>[Validate]</c>. Otherwise calls <see cref="Validate"/>; when errors are reported, returns a
/// <c>ValidateResult</c> if <typeparamref name="TResult"/> is <c>ValidateResult</c>, else throws <c>ValidateException</c>.
/// </summary>
public abstract class AbstractValidationRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    /// <inheritdoc/>
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


    /// <summary>
    /// Helper for derived validators to append an error message keyed by member name.
    /// </summary>
    protected static void AddError(string key, string error, Dictionary<string, List<string>> populate)
    {
        if (!populate.ContainsKey(key))
            populate.Add(key, new List<string>());

        populate[key].Add(error);
    }


    /// <summary>
    /// Performs the actual validation work. Add any errors discovered to <paramref name="populate"/>.
    /// </summary>
    protected abstract Task Validate(TRequest request, Dictionary<string, List<string>> populate, CancellationToken cancellationToken);
}
