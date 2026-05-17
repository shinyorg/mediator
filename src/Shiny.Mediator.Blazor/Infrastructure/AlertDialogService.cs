using Microsoft.JSInterop;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Blazor <see cref="IAlertDialogService"/> implementation that displays a message by invoking
/// the browser's built-in <c>alert()</c> function via in-process JS interop.
/// </summary>
public class AlertDialogService(IJSRuntime jsRuntime) : IAlertDialogService
{
    /// <inheritdoc/>
    public void Display(string title, string message)
        => ((IJSInProcessRuntime)jsRuntime).InvokeVoid("alert", $"{title}\n{message}");
}