using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Reflector;


public partial class SmartContractKeyProvider(
    ILogger<SmartContractKeyProvider>? logger,
    IConfiguration? configuration
) : IContractKeyProvider
{
    [GeneratedRegex("\\{([^}:]+)(:[^}]*)?\\}")]
    private static partial Regex parser();
    
    
    public string GetContractKey(object contract)
    {
        var reflector = contract.GetReflector(true)!;
        
        var type = contract.GetType();
        var configKey = $"Mediator:Keys:{type.Namespace}.{type.Name}";
        var parseKey = configuration?[configKey];
        
        // TODO: could I get the keys from an attribute or new version of IRequestKey?
        if (!String.IsNullOrWhiteSpace(parseKey))
        {
            logger?.LogDebug("Configuration key found for {ContractType}: {ParseKey}", type.FullName, parseKey);
            return ParseKey(parseKey, reflector);
        }
        return this.GetKeyFromReflection(reflector);
    }
    
    
    string ParseKey(string parseKey, IReflectorClass cls)
    {
        var info = GetParseInfo(parseKey, cls);
        var values = info.VariableNames.Select(x => cls[x]).ToList();
        
        var key = FormattableStringFactory.Create(info.FinalParseKey, values.ToArray());
        var final = key.ToString();
        return final;
    }
    
    
    readonly ConcurrentDictionary<Type, TypeParseInfo> parseInfoCache = new();
    
    TypeParseInfo GetParseInfo(string parseKey, IReflectorClass cls)
    {
        var info = this.parseInfoCache.GetOrAdd(
            cls.ReflectedObject.GetType(), 
            _ => BuildParseInfo(parseKey, cls)
        );
        return info;
    }
    
    
    TypeParseInfo BuildParseInfo(string parseKey, IReflectorClass cls)
    {
        logger?.LogDebug("Building parse info for {ContractType}: {ParseKey}", cls.ReflectedObject.GetType().FullName, parseKey);
        
        var finalParseKey = parseKey;
        var index = 0;
        var variableNames = new List<string>();
        var matches = parser().Matches(parseKey);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var variableName = match.Groups[1].Value;
                if (cls.HasProperty(variableName))
                {
                    // I don't want to replace formatting on the variable
                    finalParseKey = finalParseKey.Replace("{" + variableName, "{" + index);
                    index++;
                    variableNames.Add(variableName);
                }
            }
        }
        
        return new TypeParseInfo(finalParseKey, variableNames);
    }
    
    
    string GetKeyFromReflection(IReflectorClass reflector)
    {
        var t = reflector.ReflectedObject.GetType();
        var key = t.FullName!;
        
        foreach (var prop in reflector.Properties)
        {
            var value = reflector[prop.Name];
            if (value != null)
                key += $"_{prop.Name}_{value}";
        }
        return key;
    }
}

record TypeParseInfo(string FinalParseKey, List<string> VariableNames);