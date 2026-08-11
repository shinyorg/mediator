using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


/// <summary>
/// Default <see cref="IContractKeyProvider"/> registered by <c>AddShinyMediator</c>. Uses <see cref="IContractKey.GetKey"/>
/// when implemented; otherwise falls back to a reflection-based key built from the full type name and public property values.
/// </summary>
public class DefaultContractKeyProvider(ILogger<DefaultContractKeyProvider>? logger = null) : IContractKeyProvider
{
    /// <inheritdoc/>
    public string GetContractKey(object contract)
    {
        if (contract is IContractKey key)
        {
            var requestKey = key.GetKey();
            logger?.LogDebug("Using request key {RequestKey} for contract {ContractType}", requestKey, contract.GetType().FullName);
            return requestKey;
        }

        var reflectKey = GetKeyFromReflection(contract);
        logger?.LogDebug("Using reflection key {ReflectKey} for contract {ContractType}", reflectKey, contract.GetType().FullName);
        return reflectKey;
    }


    static string GetKeyFromReflection(object request)
    {
        var t = request.GetType();
        var key = t.FullName!;
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