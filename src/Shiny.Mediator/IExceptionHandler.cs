namespace Shiny.Mediator;

public interface IExceptionHandler
{
    /// <summary>
    /// Manage an exception from commands, requests, & events centrally
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    Task<bool> Handle(
        MediatorContext context,
        Exception exception
    );
}