namespace Shiny.Mediator.Prism;

/// <summary>
/// Class-based <see cref="IPrismRegionNavigationCommand"/> for dispatching Prism region
/// navigation through the mediator. Inherit to set <see cref="NavigationParameterName"/>.
/// </summary>
public class PrismRegionNavigationCommand(string regionName, string viewName, IRegionManager regionManager) : IPrismRegionNavigationCommand
{
    /// <inheritdoc/>
    public string RegionName => regionName;

    /// <inheritdoc/>
    public string ViewName => viewName;

    /// <inheritdoc/>
    public string? NavigationParameterName { get; protected set; }

    /// <inheritdoc/>
    public IRegionManager RegionManager => regionManager;
}


/// <summary>
/// Record-based <see cref="IPrismRegionNavigationCommand"/> for declaring Prism region navigation
/// inline without defining a dedicated command class.
/// </summary>
public record PrismRegionNavigationRecord(
    string RegionName,
    string ViewName,
    IRegionManager RegionManager,
    string? NavigationParameterName = null
) : IPrismRegionNavigationCommand;
