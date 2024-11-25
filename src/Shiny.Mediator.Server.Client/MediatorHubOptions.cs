using System.Reflection;
using System.Text.RegularExpressions;

namespace Shiny.Mediator.Server.Client;


public class MediatorHubOptions
{
    readonly Dictionary<Type, Uri> specificMaps = new();
    readonly Dictionary<string, Uri> namespaces = new();
    readonly Dictionary<Assembly, Uri> assemblies = new();
    
    // TODO: work as a white list, first found = good
    public bool TreatMissingMappingsAsErrors { get; set; } = true;
    
    
    public MediatorHubOptions Map(Assembly assembly, Uri uri)
    {
        if (this.assemblies.TryGetValue(assembly, out var existing))
            throw new InvalidOperationException($"Assembly '{assembly.FullName}' already registered");
        
        this.assemblies.Add(assembly, uri);
        return this;
    }


    /// <summary>
    /// Registers a glob/wildcard search for contract types to register against a specific URI
    /// WARNING: this uses a whitelisting (first find wins) search
    /// </summary>
    /// <param name="partialOrFullNamespace">My.Contracts.* or My.* or * (careful)</param>
    /// <param name="uri"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public MediatorHubOptions Map(string partialOrFullNamespace, Uri uri)
    {
        if (this.namespaces.TryGetValue(partialOrFullNamespace, out var existing))
            throw new InvalidOperationException($"Namespace '{partialOrFullNamespace}' already registered");
        
        this.namespaces.Add(partialOrFullNamespace, uri);
        return this;
    }
    
    
    /// <summary>
    /// Maps a contract (IRequest or IEvent) to a specific URI
    /// </summary>
    /// <param name="uri"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public MediatorHubOptions Map<T>(Uri uri)
    {
        if (!typeof(T).IsContractType())
            throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not a Mediator IEvent or IRequest");
        
        if (this.specificMaps.TryGetValue(typeof(T), out var requestType))
            throw new InvalidOperationException($"Type '{typeof(T).FullName}' already registered");
        
        this.specificMaps.Add(typeof(T), uri);
        return this;
    }
    

    public Uri? GetUriForContract(Type contractType)
    {
        if (this.specificMaps.TryGetValue(contractType, out Uri uri))
            return uri;
        
        // https://stackoverflow.com/questions/42130564/string-comparison-with-wildcard-search-in-c-sharp
        if (this.TryGetUriForContract(contractType, out Uri uri2))
            return uri2;
        
        if (this.assemblies.TryGetValue(contractType.Assembly, out Uri uri3))
            return uri3;
        
        return null;
    }


    bool TryGetUriForContract(Type contractType, out Uri uri)
    {
        uri = null;
        var ns = contractType.Namespace!;
        foreach (var map in this.namespaces)
        {
            var found = Regex.IsMatch(ns, WildCardToRegular(map.Key));
            if (found)
            {
                uri = map.Value;
                return true;
            }
        }
        return false;
    }

    static string WildCardToRegular(string value) 
        => "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
}