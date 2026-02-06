using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shiny.Mediator.SourceGenerators;


static class Extensions
{
    public static string Pascalize(this string str)
    {
        if (String.IsNullOrEmpty(str))
            return str;

        // Split by common separators (underscore, hyphen, space) and pascalize each part
        var separators = new[] { '_', '-', ' ', '.' };

        if (str.Any(c => separators.Contains(c)))
        {
            var parts = str.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var pascal = parts
                .Select(s => char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s.Substring(1).ToLower() : ""))
                .Aggregate(string.Empty, (s1, s2) => s1 + s2);
            return pascal;
        }

        if (str.All(x => char.IsUpper(x) || !char.IsLetter(x)))
        {
            var result = char.ToUpper(str[0]) + str.Substring(1).ToLower();
            return result;
        }

        if (char.IsUpper(str[0]))
            return str;
        
        var r = char.ToUpper(str[0]) + str.Substring(1);
        return r;
    }


    /// <summary>
    /// Ensures the string is a valid C# identifier by removing invalid characters
    /// and ensuring it doesn't start with a digit.
    /// </summary>
    public static string ToSafeIdentifier(this string str)
    {
        if (String.IsNullOrEmpty(str))
            return "_";

        var sb = new System.Text.StringBuilder();
        foreach (var c in str)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
            // Skip invalid characters
        }

        var result = sb.ToString();
        if (result.Length == 0)
            return "_";

        // If starts with digit, prefix with underscore
        if (char.IsDigit(result[0]))
            result = "_" + result;

        return result;
    }

    
    public static string? PriorityGetBuildProperty(this AnalyzerConfigOptions options, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            var val = options.GetBuildProperty(name);
            if (val != null)
                return val;
        }

        return null;
    }

    public static string? GetBuildProperty(this AnalyzerConfigOptions options, string propertyName, string? defaultValue = null)
    {
        if (!options.TryGetValue("build_property." + propertyName, out var value))
            return defaultValue;

        if (String.IsNullOrWhiteSpace(value))
            return defaultValue;
        
        return value;
    }
}