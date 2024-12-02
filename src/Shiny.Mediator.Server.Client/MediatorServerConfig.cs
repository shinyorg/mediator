using System.Reflection;
using System.Text.RegularExpressions;

namespace Shiny.Mediator.Server.Client;


public class MediatorServerConfig
{
    readonly Dictionary<Type, Uri> specificMaps = new();
    readonly Dictionary<string, Uri> namespaces = new();
    readonly Dictionary<Assembly, Uri> assemblies = new();
    
    // TODO: work as a white list, first found = good
    public bool TreatMissingMappingsAsErrors { get; set; } = true;


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Uri> GetUniqueUris()
    {
        var list = new List<Uri>();
        if (this.specificMaps.Count > 0)
            list.AddRange(this.specificMaps.Values);

        if (this.namespaces.Count > 0)
            list.AddRange(this.namespaces.Values);
        
        if (this.assemblies.Count > 0)
            list.AddRange(this.assemblies.Values);
        
        return list.Distinct();
    }
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="uri"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public MediatorServerConfig Map(Assembly assembly, Uri uri)
    {
        if (this.assemblies.ContainsKey(assembly))
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
    public MediatorServerConfig Map(string partialOrFullNamespace, Uri uri)
    {
        if (this.namespaces.ContainsKey(partialOrFullNamespace))
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
    public MediatorServerConfig Map<T>(Uri uri)
    {
        // if (!typeof(T).IsContractType())
        //     throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not a Mediator IEvent or IRequest");
        
        if (this.specificMaps.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"Type '{typeof(T).FullName}' already registered");
        
        this.specificMaps.Add(typeof(T), uri);
        return this;
    }
    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contractType"></param>
    /// <returns></returns>
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