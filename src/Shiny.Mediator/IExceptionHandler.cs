namespace Shiny.Mediator;

public interface IExceptionHandler
{
    /// <summary>
    /// Manage an exception from commands, requests, & events centrally
    /// </summary>
    /// <param name="message"></param>
    /// <param name="handler"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    Task<bool> Handle(
        object message,
        object handler,
        Exception exception
    );
}