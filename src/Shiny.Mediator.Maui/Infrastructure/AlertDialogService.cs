using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class AlertDialogService(ILogger<AlertDialogService> logger) : IAlertDialogService
{
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