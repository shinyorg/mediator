namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class UserNotifyAttribute : Attribute
{
    public string? ErrorMessage { get; set; }
    public string? ErrorTitle { get; set; }
}