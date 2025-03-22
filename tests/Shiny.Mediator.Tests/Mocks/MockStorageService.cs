using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Mocks;


public class MockStorageService : IStorageService
{
    readonly Dictionary<string, Dictionary<string, object>> items = new();
    
    
    public Task Set<T>(string category, string key, T value)
    {
        this.items[category] ??= new();
        this.items[category][key] = value;
        return Task.CompletedTask;
    }

    
    public Task<T?> Get<T>(string category, string key)
    {
        var result = default(T);

        if (this.items.ContainsKey(category))
        {
            var cat = this.items[category];
            if (cat.ContainsKey(key))
            {
                result = (T)cat[key];
            }
        }
        return Task.FromResult(result);
    }

    
    public Task Remove(string category, string requestKey, bool partialMatchKey = false)
    {
        // TODO: implement partialMatchKey
        if (this.items.ContainsKey(category))
        {
            this.items[category].Remove(requestKey);
        }
        return Task.CompletedTask;
    }
    

    public Task Clear(string category)
    {
        this.items.Remove(category);
        return Task.CompletedTask;
    }
}