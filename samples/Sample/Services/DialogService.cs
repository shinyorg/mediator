namespace Sample.Services;


public interface IDialogService
{
    Task DisplayAlertAsync(string title, string message, string cancel);
    Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
}


public class DialogService : IDialogService
{
    static Page CurrentPage => Shell.Current?.CurrentPage ?? Application.Current!.Windows[0].Page!;

    public Task DisplayAlertAsync(string title, string message, string cancel)
        => MainThread.InvokeOnMainThreadAsync(() => CurrentPage.DisplayAlertAsync(title, message, cancel));

    public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        => MainThread.InvokeOnMainThreadAsync(() => CurrentPage.DisplayAlertAsync(title, message, accept, cancel));
}
