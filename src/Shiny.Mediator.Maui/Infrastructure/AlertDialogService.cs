namespace Shiny.Mediator.Infrastructure;


public class AlertDialogService : IAlertDialogService
{
    public void Display(string title, string message)
        => Application.Current.MainPage.DisplayAlert(title, message, "OK");
}