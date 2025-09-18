using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Shiny.Mediator.SourceGenerators;


static class Extensions
{
    public static string Pascalize(this string str)
    {
        if (str.All(x => char.IsUpper(x) || !char.IsLetter(x)))
        {
            if (str.Contains("_"))
            {
                var pascal = str.ToLower()
                    .Split(["_"], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
                    .Aggregate(string.Empty, (s1, s2) => s1 + s2);

                return pascal;
            }

            var result = char.ToUpper(str[0]) + str.Substring(1).ToLower();
            return result;
        }

        if (char.IsUpper(str[0]))
            return str;
        
        var r = char.ToUpper(str[0]) + str.Substring(1);
        r = r.Replace("_", "");
        return r;
    }
}