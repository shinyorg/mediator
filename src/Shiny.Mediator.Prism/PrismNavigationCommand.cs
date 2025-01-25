namespace Shiny.Mediator.Prism;

public class PrismNavigationCommand(string pageUri) : IPrismNavigationCommand
{
    public string? PrependedNavigationUri { get; protected set; }
    public string PageUri => pageUri;
    public string? NavigationParameterName { get; protected set; }
    public bool? IsAnimated { get; protected set; }
    public bool IsModal { get; set; }
    public INavigationService? Navigator { get; set; }
}


public record PrismNavigationRecord(
    string PageUri,
    bool IsModal = false,
    bool? IsAnimated = null,
    string? NavigationParameterName = null,
    string? PrependedNavigationUri = null
) : IPrismNavigationCommand
{
    public INavigationService? Navigator { get; set; }
}