namespace Shiny.Mediator.Prism;

public class PrismRegionNavigationCommand(string regionName, string viewName, IRegionManager regionManager) : IPrismRegionNavigationCommand
{
    public string RegionName => regionName;
    public string ViewName => viewName;
    public string? NavigationParameterName { get; protected set; }
    public IRegionManager RegionManager => regionManager;
}


public record PrismRegionNavigationRecord(
    string RegionName,
    string ViewName,
    IRegionManager RegionManager,
    string? NavigationParameterName = null
) : IPrismRegionNavigationCommand;
