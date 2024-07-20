namespace Shiny.Mediator;

public record ValidateResult(IReadOnlyDictionary<string, IReadOnlyList<string>> Errors)
{
    public bool IsValid => !this.Errors.Any();
    public static ValidateResult Success { get; } = new(new Dictionary<string, IReadOnlyList<string>>(0));
}
