using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Avalonia <see cref="IAlertDialogService"/>. Avalonia has no built-in message box, so on desktop this
/// shows a small modal window over the active window, and on mobile/browser (single view) a centered
/// flyout over the main view. Safe to call from any thread. Register your own
/// <see cref="IAlertDialogService"/> before <c>UseAvalonia()</c> to use a dialog library instead.
/// </summary>
public class AlertDialogService(ILogger<AlertDialogService>? logger = null) : IAlertDialogService
{
    /// <summary>
    /// The text of the dismiss button. Defaults to <c>OK</c>.
    /// </summary>
    public string DismissButtonText { get; set; } = "OK";


    /// <inheritdoc/>
    public void Display(string title, string message) => Dispatcher.UIThread.Post(async () =>
    {
        try
        {
            switch (Application.Current?.ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    await this.ShowWindow(desktop, title, message);
                    break;

                case ISingleViewApplicationLifetime { MainView: { } mainView }:
                    this.ShowFlyout(mainView, title, message);
                    break;

                default:
                    logger?.LogWarning("No Avalonia window or view is available - cannot display alert '{Title}'", title);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to display alert '{Title}'", title);
        }
    });


    async Task ShowWindow(IClassicDesktopStyleApplicationLifetime desktop, string title, string message)
    {
        var owner = desktop.Windows.FirstOrDefault(x => x.IsActive) ?? desktop.MainWindow;
        var window = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = owner == null 
                ? WindowStartupLocation.CenterScreen 
                : WindowStartupLocation.CenterOwner
        };
        window.Content = this.BuildContent(title, message, () => window.Close());

        if (owner == null)
            window.Show();
        else
            await window.ShowDialog(owner);
    }


    void ShowFlyout(Control mainView, string title, string message)
    {
        var flyout = new Flyout
        {
            Placement = PlacementMode.Center,
            ShowMode = FlyoutShowMode.Standard
        };
        flyout.Content = this.BuildContent(title, message, () => flyout.Hide());
        flyout.ShowAt(mainView);
    }


    Control BuildContent(string title, string message, Action dismiss)
    {
        var button = new Button
        {
            Content = this.DismissButtonText,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true,
            IsCancel = true
        };
        button.Click += (_, _) => dismiss();

        return new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            MinWidth = 280,
            MaxWidth = 480,
            Children =
            {
                new TextBlock { Text = title, FontWeight = Avalonia.Media.FontWeight.Bold, FontSize = 18, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                button
            }
        };
    }
}
