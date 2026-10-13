using System.Diagnostics.CodeAnalysis;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Supplies reflection-free contract keys for a known set of contract types. The source generator emits one per
/// assembly when <c>ShinyMediatorGenerateContractKeys</c> is enabled, and <see cref="Impl.DefaultContractKeyProvider"/>
/// consults every registered source before falling back to reflection. Multiple sources may be registered.
/// </summary>
public interface IContractKeySource
{
    /// <summary>
    /// Returns true and the key when <paramref name="contract"/> is a type this source knows about.
    /// </summary>
    bool TryGetKey(object contract, [NotNullWhen(true)] out string? key);
}
