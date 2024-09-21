namespace Shiny.Mediator.Infrastructure;

public interface IAlertDialogService
{
    void Display(string title, string message);
}