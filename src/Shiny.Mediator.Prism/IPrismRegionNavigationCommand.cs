namespace Shiny.Mediator;

/// <summary>
/// Mediator command that, when dispatched, requests Prism region navigation to a named view.
/// Handled by the registered Prism region command handler which marshals to the UI thread and
/// calls <c>IRegionManager.RequestNavigate</c>.
/// </summary>
public interface IPrismRegionNavigationCommand : ICommand
{
    /// <summary>
    /// Name of the Prism region that will host the navigated view.
    /// </summary>
    string RegionName { get; }

    /// <summary>
    /// Registered view name to navigate to within <see cref="RegionName"/>.
    /// </summary>
    string ViewName { get; }

    /// <summary>
    /// Optional key under which this command is passed in the Prism <c>NavigationParameters</c>.
    /// Defaults to the command type name when not specified.
    /// </summary>
    string? NavigationParameterName { get; }

    /// <summary>
    /// The region manager instance used to perform the navigation request.
    /// </summary>
    IRegionManager RegionManager { get; }
}
