namespace Shiny.Mediator.Prism;

/// <summary>
/// Class-based <see cref="IPrismNavigationCommand"/> for dispatching Prism page navigation through
/// the mediator. Inherit and assign the protected setters to customise prepended URI, animation,
/// or navigation parameter name.
/// </summary>
public class PrismNavigationCommand(string pageUri) : IPrismNavigationCommand
{
    /// <inheritdoc/>
    public string? PrependedNavigationUri { get; protected set; }

    /// <inheritdoc/>
    public string PageUri => pageUri;

    /// <inheritdoc/>
    public string? NavigationParameterName { get; protected set; }

    /// <inheritdoc/>
    public bool? IsAnimated { get; protected set; }

    /// <inheritdoc/>
    public bool IsModal { get; set; }

    /// <inheritdoc/>
    public INavigationService? Navigator { get; set; }
}


/// <summary>
/// Record-based <see cref="IPrismNavigationCommand"/> for declaring Prism page navigation inline
/// (e.g. <c>new PrismNavigationRecord("MainPage", IsModal: true)</c>) without defining a dedicated
/// command class.
/// </summary>
public record PrismNavigationRecord(
    string PageUri,
    bool IsModal = false,
    bool? IsAnimated = null,
    string? NavigationParameterName = null,
    string? PrependedNavigationUri = null
) : IPrismNavigationCommand
{
    /// <inheritdoc/>
    public INavigationService? Navigator { get; set; }
}