namespace Shiny.Mediator;

public interface IPrismNavigationRequest : IRequest
{
    string PageUri { get; }
    string? NavigationParameterName { get; }
    
    /// <summary>
    /// Pass the navigation service from your viewmodel for it to be used
    /// </summary>
    INavigationService? Navigator { get; set; }
}