namespace Shiny.Mediator.Prism;

public class PrismRegionNavigationCommand(string regionName, string viewName) : IPrismRegionNavigationCommand
{
    public string RegionName => regionName;
    public string ViewName => viewName;
    public string? NavigationParameterName { get; protected set; }
    public IRegionManager? RegionManager { get; set; }
}


public record PrismRegionNavigationRecord(
    string RegionName,
    string ViewName,
    string? NavigationParameterName = null
) : IPrismRegionNavigationCommand
{
    public IRegionManager? RegionManager { get; set; }
}
