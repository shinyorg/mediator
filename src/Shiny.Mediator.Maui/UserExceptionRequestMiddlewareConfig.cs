namespace Shiny.Mediator;

public class UserExceptionRequestMiddlewareConfig
{
    public bool ShowFullException { get; set; }
    public string ErrorMessage { get; set; } = "We're sorry. An error has occurred";
    public string ErrorTitle { get; set; } = "Error";
    public string ErrorConfirm { get; set; } = "OK";
}