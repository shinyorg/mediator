namespace Shiny.Mediator;

// TODO: what about dialogs?
public interface IPrismNavigationRequest : IRequest
{
    // contract can choose to open these up
    string? PrependedNavigationUri { get; }
    string PageUri { get; }
    string? NavigationParameterName { get; }
    bool? IsAnimated { get; }  
    bool IsModal { get; }
    

    /// <summary>
    /// Pass the navigation service from your viewmodel for it to be used
    /// </summary>
    INavigationService? Navigator { get; set; }
}