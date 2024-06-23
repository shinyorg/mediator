namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ReplayAttribute : Attribute {}

public interface IReplayKeyProvider<TRequest, TResult> : IStreamRequestHandler<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    string GetReplayKey(TRequest request);
}
