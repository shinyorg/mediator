namespace Shiny.Mediator;

public class ExecutionContext(IRequestHandler handler, CancellationToken cancellationToken)
{
    readonly Dictionary<string, object> store = new();
    
    public Guid ExecutionId { get; }= Guid.NewGuid();
    public IReadOnlyDictionary<string, object> Values => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);
    
    public IRequestHandler RequestHandler => handler;
    public CancellationToken CancellationToken => cancellationToken;
}

public class ExecutionContext<TRequest>(
    TRequest request, 
    IRequestHandler handler, 
    CancellationToken cancellationToken
) : ExecutionContext(handler, cancellationToken)
{
    public TRequest Request => request;
}