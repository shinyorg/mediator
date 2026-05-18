namespace Shiny.Mediator;

/// <summary>
/// Thrown by the validation middleware when a contract marked with <see cref="ValidateAttribute"/>
/// fails data annotation validation. The <see cref="Result"/> property exposes the per-property errors.
/// </summary>
public class ValidateException(ValidateResult result, string? message = null) : Exception(message ?? "Validation failed")
{
    /// <summary>
    /// The validation result containing the per-property error details that triggered the exception.
    /// </summary>
    public ValidateResult Result => result;
}
