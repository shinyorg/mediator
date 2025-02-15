using Uno.Extensions.Navigation;

namespace Shiny.Mediator.Infrastructure;


public class AlertDialogService(INavigator navigator) : IAlertDialogService
{
    public async void Display(string title, string message)
    {
        try
        {
            await navigator.ShowMessageDialogAsync(
                this,
                title: title, 
                content: message
            );
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}