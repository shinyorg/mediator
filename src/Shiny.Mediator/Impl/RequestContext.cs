namespace Shiny.Mediator.Impl;

public class RequestContext(IRequestHandler handler) : IRequestContext
{
    readonly Dictionary<string, object> store = new();
    
    public Guid ExecutionId { get; }= Guid.NewGuid();
    public IReadOnlyDictionary<string, object> Values => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);

    public IRequestHandler RequestHandler => handler;
    // public object Request { get; }
    // public object? ReturnValue { get; set; }

    // public TResult? Result { get; set; }
}