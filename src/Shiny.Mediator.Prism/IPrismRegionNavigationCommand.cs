namespace Shiny.Mediator;

public interface IPrismRegionNavigationCommand : ICommand
{
    string RegionName { get; }
    string ViewName { get; }
    string? NavigationParameterName { get; }

    /// <summary>
    /// Pass the region manager from your viewmodel for it to be used
    /// </summary>
    IRegionManager? RegionManager { get; set; }
}
