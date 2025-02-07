using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace Shiny.Mediator.Infrastructure;


public class AlertDialogService(ILogger<AlertDialogService> logger) : IAlertDialogService
{
    public void Display(string title, string message)
    {
        // TODO: could use acr userdialogs
        // ContentDialog deleteFileDialog = new ContentDialog
        // {
        //     Title = "Delete file permanently?",
        //     Content = "If you delete this file, you won't be able to recover it. Do you want to delete it?",
        //     PrimaryButtonText = "Delete",
        //     CloseButtonText = "Cancel"
        // };
        //
        // deleteFileDialog.XamlRoot = anyLoadedControl.XamlRoot;
        //
        // ContentDialogResult result = await deleteFileDialog.ShowAsync();
    }
}