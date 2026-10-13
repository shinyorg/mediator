using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;

namespace Shiny.Mediator.SourceGenerators;


/// <summary>A component schema carrying a discriminator - generated as an abstract base class.</summary>
public class OpenApiPolymorphicBase(string componentName, string propertyName)
{
    public string ComponentName { get; } = componentName;

    /// <summary>The JSON name of the discriminator property.</summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>Discriminator value → variant component name, in declaration order.</summary>
    public List<KeyValuePair<string, string>> Variants { get; } = new();
}


/// <summary>A concrete type of a polymorphic hierarchy.</summary>
public class OpenApiPolymorphicVariant(OpenApiPolymorphicBase root, string tag, string parentComponent)
{
    public OpenApiPolymorphicBase Root { get; } = root;
    public string Tag { get; } = tag;

    /// <summary>The component the generated class derives from (the root, or an intermediate variant).</summary>
    public string ParentComponent { get; } = parentComponent;
}


/// <summary>
/// Resolves discriminated hierarchies across component schemas. Two shapes are supported:
/// <list type="bullet">
///   <item>Union style (ASP.NET Core <c>Microsoft.AspNetCore.OpenApi</c>): the base declares
///   <c>oneOf</c>/<c>anyOf</c> of <c>$ref</c>s plus a <c>discriminator</c>.</item>
///   <item>Inheritance style (NSwag / Swashbuckle): the base declares the <c>discriminator</c> and
///   derived schemas use <c>allOf: [$ref Base, {...}]</c>.</item>
/// </list>
/// Variants missing from <c>discriminator.mapping</c> use their component name as the value (the
/// implicit mapping defined by the spec).
/// </summary>
public class OpenApiPolymorphism
{
    public Dictionary<string, OpenApiPolymorphicBase> Bases { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, OpenApiPolymorphicVariant> Variants { get; } = new(StringComparer.Ordinal);


    public static OpenApiPolymorphism Analyze(IDictionary<string, IOpenApiSchema> schemas, Action<string> warn)
    {
        var result = new OpenApiPolymorphism();
        var candidates = schemas
            .Where(x => !String.IsNullOrWhiteSpace(x.Value.Discriminator?.PropertyName))
            .Select(x => x.Key)
            .ToList();

        foreach (var name in candidates)
        {
            // an intermediate type re-declaring the discriminator belongs to its root's hierarchy
            if (GetAncestors(name, schemas).Any(candidates.Contains))
                continue;

            var schema = schemas[name];
            var info = new OpenApiPolymorphicBase(name, schema.Discriminator!.PropertyName!);
            var tags = new List<KeyValuePair<string, string>>();
            var mapped = new HashSet<string>(StringComparer.Ordinal);

            if (schema.Discriminator.Mapping != null)
            {
                foreach (var mapping in schema.Discriminator.Mapping)
                {
                    var target = GetReferenceId(mapping.Value);
                    if (target == null || !schemas.ContainsKey(target))
                    {
                        warn($"Discriminator mapping '{mapping.Key}' on '{name}' does not reference a component schema and was ignored");
                        continue;
                    }
                    if (target == name)
                    {
                        warn($"Discriminator mapping '{mapping.Key}' on '{name}' maps to the base itself - polymorphic bases are generated abstract, so the value was ignored");
                        continue;
                    }
                    if (mapped.Add(target))
                        tags.Add(new(mapping.Key, target));
                }
            }

            // implicit mapping - union branches + everything inheriting from the base via allOf
            var implicitVariants = (schema.OneOf ?? Enumerable.Empty<IOpenApiSchema>())
                .Concat(schema.AnyOf ?? Enumerable.Empty<IOpenApiSchema>())
                .Select(GetReferenceId)
                .Where(x => x != null && x != name && schemas.ContainsKey(x))
                .Concat(schemas.Keys.Where(x => x != name && GetAncestors(x, schemas).Contains(name)));

            foreach (var variant in implicitVariants)
            {
                if (!mapped.Add(variant!))
                    continue;

                if (tags.Any(x => x.Key == variant))
                    warn($"'{variant}' implicitly uses discriminator value '{variant}' on '{name}', which the mapping already assigns to another schema - add it to the mapping");
                else
                    tags.Add(new(variant!, variant!));
            }

            foreach (var tag in tags)
            {
                if (result.Variants.ContainsKey(tag.Value))
                {
                    warn($"'{tag.Value}' is a variant of more than one discriminated base - it will only derive from '{result.Variants[tag.Value].Root.ComponentName}'");
                    continue;
                }

                // an allOf parent must be the base or another member of this hierarchy (C# single inheritance)
                var parent = GetParent(tag.Value, schemas) ?? name;
                if (parent != name && !mapped.Contains(parent))
                {
                    warn($"'{tag.Value}' derives from '{parent}' which is not part of the '{name}' hierarchy - it was not generated as a variant");
                    continue;
                }

                info.Variants.Add(tag);
                result.Variants.Add(tag.Value, new OpenApiPolymorphicVariant(info, tag.Key, parent));
            }

            if (info.Variants.Count > 0)
                result.Bases.Add(name, info);
        }

        return result;
    }


    /// <summary>The single component referenced from <c>allOf</c> - the schema's parent type.</summary>
    public static string? GetParent(string name, IDictionary<string, IOpenApiSchema> schemas)
    {
        if (!schemas.TryGetValue(name, out var schema) || schema.AllOf == null)
            return null;

        var refs = schema.AllOf
            .Select(GetReferenceId)
            .Where(x => x != null && schemas.ContainsKey(x))
            .ToList();

        return refs.Count == 1 ? refs[0] : null;
    }


    static IEnumerable<string> GetAncestors(string name, IDictionary<string, IOpenApiSchema> schemas)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { name };
        var parent = GetParent(name, schemas);
        while (parent != null && visited.Add(parent))
        {
            yield return parent;
            parent = GetParent(parent, schemas);
        }
    }


    public static string? GetReferenceId(IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchemaReference reference)
            return null;

        var id = reference.Reference.Id;
        if (String.IsNullOrWhiteSpace(id))
            return null;

        // mapping values may be full "#/components/schemas/Name" pointers or bare names
        return id!.Split('/').Last();
    }
}
