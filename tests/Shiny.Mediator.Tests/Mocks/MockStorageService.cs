using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Mocks;


public class MockStorageService : IStorageService
{
    public Dictionary<string, Dictionary<string, object>> Items { get; } = new();
    
    
    public Task Set<T>(string category, string key, T value, CancellationToken cancellationToken = default)
    {
        if (!this.Items.ContainsKey(category))
            this.Items.Add(category, new Dictionary<string, object>());
        
        this.Items[category][key] = value;
        return Task.CompletedTask;
    }

    
    public Task<T?> Get<T>(string category, string key, CancellationToken cancellationToken = default)
    {
        var result = default(T);

        if (this.Items.ContainsKey(category))
        {
            var cat = this.Items[category];
            if (cat.ContainsKey(key))
            {
                result = (T)cat[key];
            }
        }
        return Task.FromResult(result);
    }

    
    public Task Remove(string category, string requestKey, bool partialMatchKey = false, CancellationToken cancellationToken = default)
    {
        // TODO: implement partialMatchKey
        if (this.Items.ContainsKey(category))
        {
            this.Items[category].Remove(requestKey);
        }
        return Task.CompletedTask;
    }
    

    public Task Clear(string category, CancellationToken cancellationToken)
    {
        this.Items.Remove(category);
        return Task.CompletedTask;
    }
}