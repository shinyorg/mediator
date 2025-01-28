namespace Shiny.Mediator.Infrastructure;


public interface IInternetService
{
    event EventHandler<bool> StateChanged;
    bool IsAvailable { get; }
    Task WaitForAvailable(CancellationToken cancelToken = default);
}