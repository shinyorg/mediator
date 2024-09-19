namespace Shiny.Mediator.Infrastructure;


public interface IInternetService
{
    bool IsAvailable { get; }
    Task WaitForAvailable(CancellationToken cancelToken = default);
}