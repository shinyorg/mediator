namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class ContractKeyAttribute(string? FormatKey = null) : Attribute;