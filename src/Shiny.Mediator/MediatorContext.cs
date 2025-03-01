using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;


public class MediatorContext
{
    public MediatorContext(    
        IServiceScope scope,
        object message, 
        ActivitySource activitySource,
        params IEnumerable<(string Key, object Value)> headers
    )
    {
        this.Message = message;
        this.ServiceScope = scope;
        this.ActivitySource = activitySource;
        foreach (var header in headers)
            this.Add(header.Key, header.Value);
    }
    
    
    public Guid Id { get; } = Guid.NewGuid();
    public IServiceScope ServiceScope { get; }
    public ActivitySource ActivitySource { get; }
    public object Message { get; }
    public object? MessageHandler { get; set; }
    
    Dictionary<string, object> store = new();
    public IReadOnlyDictionary<string, object> Headers => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);
    
    public MediatorContext? Parent { get; private set; }

    readonly List<MediatorContext> children = new();
    public List<MediatorContext> ChildContexts { get; private set; } = new();

    
    // TODO: should be thread safe for event publishing
    public MediatorContext CreateChild(object? newMessage)
    {
        var msg = newMessage ?? this.Message;
        var newContext = new MediatorContext(this.ServiceScope, msg, this.ActivitySource)
        {
            Parent = this,
            store = this.store.ToDictionary() // copy over
        };
        this.children.Add(newContext);
        return newContext;
    }
    

    public Activity? StartActivity(string activityName)
    {
        var activity = this.ActivitySource?.StartActivity(activityName);
        if (activity != null)
        {
            activity.SetTag("operation_id", this.Id);
            foreach (var header in this.Headers)
                activity.SetTag(header.Key, header.Value);
        }
        return activity;
    }
    
    
    public T? TryGetValue<T>(string key)
    {
        if (this.Headers.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
}