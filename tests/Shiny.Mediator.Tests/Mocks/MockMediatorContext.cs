using System.Diagnostics;

namespace Shiny.Mediator.Tests.Mocks;

public class MockMediatorContext : IMediatorContext
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public IServiceScope ServiceScope { get; set; }
    public ActivitySource ActivitySource { get; set; }
    public object Message { get; set; }
    public object? MessageHandler { get; set; }
    
    public Dictionary<string, object> OpenHeaders { get; } = new();
    public IReadOnlyDictionary<string, object> Headers => this.OpenHeaders;
    public void AddHeader(string key, object value) => this.OpenHeaders.Add(key, value);

    public IMediatorContext? Parent { get; set; }

    public List<IMediatorContext> OpenChildContext { get; } = new();
    public IReadOnlyList<IMediatorContext> ChildContexts => this.OpenChildContext;
    public DateTimeOffset CreatedAt { get; set; }
    public bool BypassExceptionHandlingEnabled { get; set; }
    public bool BypassMiddlewareEnabled { get; set; }
    public IMediatorContext CreateChild(object? newMessage)
    {
        throw new NotImplementedException();
    }

    public Activity? StartActivity(string activityName) => null;

    public T? TryGetValue<T>(string key)
    {
        if (this.OpenHeaders.TryGetValue(key, out var value))
        {
            if (value is T result)
                return result;
        }

        return default;
    }
    

    public Task<TResult> Request<TResult>(
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default, 
        Action<IMediatorContext>? configure = null
    )
    {
        throw new NotImplementedException();
    }

    public Task Send<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default, 
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand
    {
        throw new NotImplementedException();
    }

    public Task Publish<TEvent>(
        TEvent @event, 
        bool executeInParallel = true, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        throw new NotImplementedException();
    }
}