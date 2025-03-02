using System.Reflection;

namespace Shiny.Mediator.Middleware;

public abstract class AbstractValidationCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(IMediatorContext context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (context.Message!.GetType().GetCustomAttribute<ValidateAttribute>() == null)
        {
            await next();
            return;
        }
        var values = new Dictionary<string, List<string>>();
        await this.Validate((TCommand)context.Message, values, cancellationToken).ConfigureAwait(false);
        
        if (values.Count == 0)
        {
            await next().ConfigureAwait(false);
            return;
        }

        var dict = (IReadOnlyDictionary<string, IReadOnlyList<string>>)values.ToDictionary(
            x => x.Key,
            x => (IReadOnlyList<string>)x.Value
        );
        var validationResults = new ValidateResult(dict);
        
        throw new ValidateException(validationResults);
    }
    
    protected static void AddError(string key, string error, Dictionary<string, List<string>> populate)
    {
        if (!populate.ContainsKey(key))
            populate.Add(key, new List<string>());

        populate[key].Add(error);
    }
    
    protected abstract Task Validate(TCommand command, Dictionary<string, List<string>> populate, CancellationToken cancellationToken);
}