namespace Shiny.Mediator;

public interface IPrismRegionNavigationCommand : ICommand
{
    string RegionName { get; }
    string ViewName { get; }
    string? NavigationParameterName { get; }

    /// <summary>
    /// The region manager instance to use for navigation
    /// </summary>
    IRegionManager RegionManager { get; }
}
