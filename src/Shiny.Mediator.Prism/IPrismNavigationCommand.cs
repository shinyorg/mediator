namespace Shiny.Mediator;

// TODO: what about dialogs?
/// <summary>
/// Mediator command that, when dispatched, performs Prism page navigation using either the
/// command's own <see cref="Navigator"/> or the registered <see cref="Shiny.Mediator.Prism.IGlobalNavigationService"/>.
/// </summary>
public interface IPrismNavigationCommand : ICommand
{
    /// <summary>
    /// Optional URI fragment prepended to <see cref="PageUri"/> (e.g. <c>"NavigationPage/"</c>)
    /// to compose the final Prism navigation URI.
    /// </summary>
    string? PrependedNavigationUri { get; }

    /// <summary>
    /// Registered Prism page URI to navigate to.
    /// </summary>
    string PageUri { get; }

    /// <summary>
    /// Optional key under which this command is passed in the Prism <c>NavigationParameters</c>.
    /// Defaults to the command type name when not specified.
    /// </summary>
    string? NavigationParameterName { get; }

    /// <summary>
    /// When set, controls whether the navigation transition is animated; <c>null</c> defers to
    /// the platform default.
    /// </summary>
    bool? IsAnimated { get; }

    /// <summary>
    /// Indicates whether the target page is pushed modally.
    /// </summary>
    bool IsModal { get; }

    /// <summary>
    /// Pass the navigation service from your viewmodel for it to be used. When <c>null</c>, the
    /// handler falls back to the resolved <see cref="Shiny.Mediator.Prism.IGlobalNavigationService"/>.
    /// </summary>
    INavigationService? Navigator { get; set; }
}