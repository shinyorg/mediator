namespace Shiny.Mediator;

public interface IRequestContext
{
    Guid ExecutionId { get; }
    IReadOnlyDictionary<string, object> Values { get; }
    void Add(string key, object value);
    
    IRequestHandler RequestHandler { get; }
}

// TODO: middleware could ref the request handler, CT, request - all from the context
// TODO: result could/should(?) come back on context?  how to protect it?
// TODO: RequestStreamContext OR reuse IRequestContext?
// public class EventContext
// {
//     // TODO: event types that were fired
//     // TODO: what about middleware that fired per event?
//     public Guid ExecutionId { get; } = Guid.NewGuid();
//     public IReadOnlyDictionary<string, object> Store => this.store;
//     
//     readonly Dictionary<string, object> store = new();
//     public void Add(string key, object value)
//         => this.store.Add(key, value);
// }