namespace Shiny.Mediator;

/// <summary>
/// Describes the outcome of validating a contract, exposing the per-property error messages produced
/// by the validation middleware. Use <see cref="Success"/> when no errors are present.
/// </summary>
/// <param name="Errors">A map from property name to the list of validation error messages for that property.</param>
public record ValidateResult(IReadOnlyDictionary<string, IReadOnlyList<string>> Errors)
{
    /// <summary>
    /// True when <see cref="Errors"/> contains no entries; otherwise false.
    /// </summary>
    public bool IsValid => !this.Errors.Any();

    /// <summary>
    /// A shared, empty result instance representing a successful (error-free) validation.
    /// </summary>
    public static ValidateResult Success { get; } = new(new Dictionary<string, IReadOnlyList<string>>(0));
}
