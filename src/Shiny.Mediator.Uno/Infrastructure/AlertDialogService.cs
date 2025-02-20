using Windows.UI.Popups;

namespace Shiny.Mediator.Infrastructure;


public class AlertDialogService : IAlertDialogService
{
    public async void Display(string title, string message)
    {
        var dialog = new MessageDialog(message, title);
        dialog.Commands.Add(new UICommand("OK"));
        await dialog.ShowAsync();
    }
}