namespace Shiny.Mediator.Prism;


/// <summary>
/// Application-wide Prism <see cref="INavigationService"/> resolved against the current window
/// and active page's container. Used by the Prism navigation command handler when a command does
/// not supply its own <see cref="IPrismNavigationCommand.Navigator"/>.
/// </summary>
public interface IGlobalNavigationService : INavigationService
{
}