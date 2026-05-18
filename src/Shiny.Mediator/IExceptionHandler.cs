namespace Shiny.Mediator;

/// <summary>
/// Centralized handler invoked when a command, request, or event throws. Multiple handlers can be registered
/// and are tried in order; the first one to return <c>true</c> stops propagation.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Inspects an exception thrown during a mediator execution. Return <c>true</c> if the exception has been
    /// handled and should not bubble; return <c>false</c> to allow the next handler (or the caller) to receive it.
    /// </summary>
    Task<bool> Handle(
        IMediatorContext context,
        Exception exception
    );
}
