namespace Shiny.Mediator;

/// <summary>
/// Marks a contract (class or struct) so a source generator emits an <see cref="IContractKey"/>
/// implementation. Supply an optional composite format string (referencing the contract's properties)
/// to control how the generated key is composed for cache, replay, and similar middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class ContractKeyAttribute(string? FormatKey = null) : Attribute;
