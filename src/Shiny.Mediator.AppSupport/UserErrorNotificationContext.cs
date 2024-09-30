namespace Shiny.Mediator;

public record UserErrorNotificationContext(
    Exception Exception,
    string Title,
    string Message
);