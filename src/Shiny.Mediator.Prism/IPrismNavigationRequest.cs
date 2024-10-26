namespace Shiny.Mediator;

public interface IPrismNavigationRequest : IRequest
{
    string PageUri { get; }
    string? NavigationParameterName { get; }
    bool? IsAnimated { get; }  
    bool IsModal { get; }
    

    /// <summary>
    /// Pass the navigation service from your viewmodel for it to be used
    /// </summary>
    INavigationService? Navigator { get; set; }
}