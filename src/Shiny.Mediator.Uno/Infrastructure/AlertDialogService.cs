using Windows.UI.Popups;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Uno <see cref="IAlertDialogService"/> implementation that displays a <see cref="MessageDialog"/>
/// with a single OK button using the Windows <see cref="Windows.UI.Popups"/> APIs.
/// </summary>
public class AlertDialogService : IAlertDialogService
{
    /// <inheritdoc/>
    public async void Display(string title, string message)
    {
        var dialog = new MessageDialog(message, title);
        dialog.Commands.Add(new UICommand("OK"));
        await dialog.ShowAsync();
    }
}