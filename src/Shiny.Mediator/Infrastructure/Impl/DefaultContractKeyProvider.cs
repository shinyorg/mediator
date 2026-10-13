using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


/// <summary>
/// Default <see cref="IContractKeyProvider"/> registered by <c>AddShinyMediator</c>. Resolution order:
/// <see cref="IContractKey.GetKey"/> on the contract, then any registered <see cref="IContractKeySource"/>
/// (emitted by the source generator when <c>ShinyMediatorGenerateContractKeys</c> is enabled), and finally a
/// reflection-based key built from the full type name and public property values. The reflection fallback can be
/// removed (and trimmed away) by setting the <see cref="ReflectionKeysSwitchName"/> runtime switch to false.
/// </summary>
public class DefaultContractKeyProvider(
    ILogger<DefaultContractKeyProvider>? logger = null,
    IEnumerable<IContractKeySource>? keySources = null
) : IContractKeyProvider
{
    /// <summary>
    /// AppContext switch controlling the reflection fallback. Set via the <c>ShinyMediatorReflectionContractKeys</c>
    /// MSBuild property; when false the trimmer removes the reflection path entirely.
    /// </summary>
    public const string ReflectionKeysSwitchName = "Shiny.Mediator.ReflectionContractKeys";

    /// <summary>
    /// True (default) when contracts without <see cref="IContractKey"/> or an <see cref="IContractKeySource"/> may be keyed via reflection.
    /// </summary>
    [FeatureSwitchDefinition(ReflectionKeysSwitchName)]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    [UnconditionalSuppressMessage("Trimming", "IL4000", Justification = "AppContext-backed guard - ILLink removes the reflection path when the switch is false, and keeps (and warns on) it when the switch is unset")]
    public static bool IsReflectionKeysSupported
        => !AppContext.TryGetSwitch(ReflectionKeysSwitchName, out var enabled) || enabled;

    readonly IContractKeySource[] sources = keySources?.ToArray() ?? [];


    /// <inheritdoc/>
    public string GetContractKey(object contract)
    {
        if (contract is IContractKey key)
        {
            var requestKey = key.GetKey();
            logger?.LogDebug("Using request key {RequestKey} for contract {ContractType}", requestKey, contract.GetType().FullName);
            return requestKey;
        }

        foreach (var source in this.sources)
        {
            if (source.TryGetKey(contract, out var sourceKey))
            {
                logger?.LogDebug("Using generated key {GeneratedKey} for contract {ContractType}", sourceKey, contract.GetType().FullName);
                return sourceKey;
            }
        }

        if (IsReflectionKeysSupported)
        {
            var reflectKey = GetKeyFromReflection(contract);
            logger?.LogDebug("Using reflection key {ReflectKey} for contract {ContractType}", reflectKey, contract.GetType().FullName);
            return reflectKey;
        }

        throw new InvalidOperationException(
            $"No contract key available for '{contract.GetType().FullName}' and reflection-based keys are disabled " +
            $"({ReflectionKeysSwitchName}=false). Implement IContractKey, apply [ContractKey], or give the contract a " +
            "[MediatorSingleton]/[MediatorScoped] handler in a project with ShinyMediatorGenerateContractKeys=true."
        );
    }


    [RequiresUnreferencedCode(
        "Builds the key from the contract's public properties via reflection - properties removed by the trimmer are " +
        "silently left out of the key. Enable ShinyMediatorGenerateContractKeys, implement IContractKey, or apply [ContractKey] instead."
    )]
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
