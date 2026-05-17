namespace Shiny.Mediator.Infrastructure;

/// <summary>
/// Platform abstraction for presenting a modal alert dialog to the user. Implemented by the host UI framework
/// (e.g. MAUI) and used by <see cref="UserNotificationExceptionHandler"/> to surface handler errors.
/// </summary>
public interface IAlertDialogService
{
    /// <summary>
    /// Displays an alert with the given title and message on the active UI.
    /// </summary>
    void Display(string title, string message);
}