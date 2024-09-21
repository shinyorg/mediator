using Microsoft.JSInterop;

namespace Shiny.Mediator.Infrastructure;


public class AlertDialogService(IJSRuntime jsRuntime) : IAlertDialogService
{
    public void Display(string title, string message)
        => ((IJSInProcessRuntime)jsRuntime).InvokeVoid("alert", $"{title}\n{message}");
}