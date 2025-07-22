using System.Reflection;

namespace Shiny.Mediator.Infrastructure.Impl;

// TODO: there can only be one!
public class DefaultContractKeyProvider : IContractKeyProvider
{
    public string GetContractKey(object contract)
    {
        if (contract is IRequestKey key)
            return key.GetKey();
        
        return GetKeyFromReflection(contract);
        
        // var t = obj.GetType();
        // var stringKey = $"{t.Namespace}_{t.Name}";
        // return stringKey;
        
        // TODO: source generate how it is built
        // TODO: from configuration - "Namespace.ClassName": "{FirstName}-{Date1}"
        // TODO: use reflector
    }
    

    static string GetKeyFromReflection(object request)
    {
        var t = request.GetType();
        var key = request.GetType().FullName!;
        var props = t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead)
            .OrderBy(x => x.Name)
            .ToList();

        foreach (var prop in props)
        {
            var value = prop.GetValue(request);
            if (value != null)
                key += $"_{prop.Name}_{value}";
        }

        return key;
    }
}