using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// MAUI <see cref="IAlertDialogService"/> implementation that shows alerts via
/// <see cref="Page.DisplayAlert(string, string, string)"/> on the first window's current page.
/// </summary>
public class AlertDialogService(ILogger<AlertDialogService>? logger = null) : IAlertDialogService
{
    /// <inheritdoc/>
    public void Display(string title, string message)
    {
        var app = Application.Current;
        if (app == null)
            return;

        var window = app.Windows.FirstOrDefault();
        if (window?.Page == null)
            return;

        window.Page
            .DisplayAlert(title, message, "OK")
            .RunInBackground(logger);
    }
}