namespace Shiny.Mediator;

/// <summary>
/// Marks a contract (command, request, or event) class for data annotation validation.
/// When present, the validation middleware will validate the contract's properties using
/// <see cref="System.ComponentModel.DataAnnotations"/> attributes before the handler executes.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ValidateAttribute : Attribute { }