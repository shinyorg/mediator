namespace Shiny.Mediator;

public class ValidateException(ValidateResult result, string? message = null) : Exception(message ?? "Validation failed")
{
    public ValidateResult Result => result;
}