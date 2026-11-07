using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Uno Platform <see cref="IAlertDialogService"/> that shows a <see cref="ContentDialog"/> with a single
/// OK button on the main tracked window. Safe to call from any thread.
/// </summary>
public class AlertDialogService(
    UnoWindowTracker windowTracker,
    ILogger<AlertDialogService>? logger = null
) : IAlertDialogService
{
    /// <summary>
    /// The text of the dialog's close button. Defaults to <c>OK</c>.
    /// </summary>
    public string CloseButtonText { get; set; } = "OK";


    /// <inheritdoc/>
    public void Display(string title, string message)
    {
        var window = windowTracker.MainWindow;
        if (window == null)
        {
            logger?.LogWarning("No Uno window is tracked - cannot display alert '{Title}'", title);
            return;
        }

        var queued = window.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                var xamlRoot = window.Content?.XamlRoot;
                if (xamlRoot == null)
                {
                    logger?.LogWarning("The Uno window has no content yet - cannot display alert '{Title}'", title);
                    return;
                }

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = this.CloseButtonText,
                    XamlRoot = xamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                // only one ContentDialog can be open per XamlRoot
                logger?.LogWarning(ex, "Failed to display alert '{Title}'", title);
            }
        });

        if (!queued)
            logger?.LogWarning("The UI dispatcher refused the alert '{Title}'", title);
    }
}
