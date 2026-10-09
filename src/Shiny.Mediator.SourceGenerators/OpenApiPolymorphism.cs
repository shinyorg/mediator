using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;

namespace Shiny.Mediator.SourceGenerators;

internal sealed class OpenApiPolymorphismException(string message) : Exception(message);

internal sealed class OpenApiPolymorphicBase(string name, IOpenApiSchema schema, string discriminator)
{
    public string Name { get; } = name;
    public IOpenApiSchema Schema { get; } = schema;
    public string Discriminator { get; } = discriminator;
    public IDictionary<string, string> Variants { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal);
}

internal sealed class OpenApiPolymorphicVariant(OpenApiPolymorphicBase parent, string discriminatorValue)
{
    public OpenApiPolymorphicBase Parent { get; } = parent;
    public string DiscriminatorValue { get; } = discriminatorValue;
}

/// <summary>
/// Resolves discriminated component unions before any source is emitted. An unsupported shape
/// must fail the build: treating a union as an empty DTO silently destroys request/response data.
/// </summary>
internal sealed class OpenApiPolymorphism
{
    public IDictionary<string, OpenApiPolymorphicBase> Bases { get; } = new Dictionary<string, OpenApiPolymorphicBase>(StringComparer.Ordinal);
    public IDictionary<string, OpenApiPolymorphicVariant> Variants { get; } = new Dictionary<string, OpenApiPolymorphicVariant>(StringComparer.Ordinal);

    public void Analyze(IDictionary<string, IOpenApiSchema> schemas, bool generateConverters)
    {
        foreach (var component in schemas)
        {
            var schema = component.Value;
            if (schema.Discriminator is not { } discriminator)
                continue;

            void Fail(string reason) => throw new OpenApiPolymorphismException($"Discriminator on '{component.Key}': {reason}");

            if (!generateConverters)
                Fail("ShinyMediatorOpenApiPolymorphism requires GenerateJsonConverters=\"true\" on this MediatorHttp item.");
            if (String.IsNullOrEmpty(discriminator.PropertyName) || discriminator.Mapping?.Count is not > 0)
                Fail("a propertyName and an explicit, complete mapping are required.");
            if (schema.Type != null && schema.Type != JsonSchemaType.Object)
                Fail("the base must be an object union; model nullable use sites as separate nullable properties.");
            if (schema.AllOf?.Count > 0 || (schema.OneOf?.Count > 0 && schema.AnyOf?.Count > 0))
                Fail("use one oneOf or anyOf union of local component references, without allOf on its base.");

            var branches = schema.OneOf?.Count > 0 ? schema.OneOf : schema.AnyOf;
            if (branches?.Count is not > 0)
                Fail("the base must declare oneOf or anyOf variants.");

            var branchNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var branch in branches!)
            {
                var branchName = LocalName(branch, schemas, component.Key);
                if (!branchNames.Add(branchName))
                    Fail($"component '{branchName}' appears more than once in the union.");
            }

            var baseName = ClassName(component.Key);
            EnsureUniqueName(baseName, schemas);
            var info = new OpenApiPolymorphicBase(baseName, schema, discriminator.PropertyName!);
            var mappedBranches = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapping in discriminator.Mapping!)
            {
                var mappedName = LocalName(mapping.Value, schemas, component.Key);
                if (!branchNames.Contains(mappedName) || !mappedBranches.Add(mappedName))
                    Fail($"mapping '{mapping.Key}' must name one distinct variant declared in the union.");

                var variantSchema = schemas[mappedName];
                if (variantSchema.Discriminator != null || variantSchema.OneOf?.Count > 0 || variantSchema.AnyOf?.Count > 0)
                    Fail($"nested polymorphic variant '{mappedName}' is not supported; use independent hierarchies.");
                if (variantSchema.AllOf?.Count > 0)
                    Fail($"variant '{mappedName}' must expose its properties directly, without allOf.");
                if (variantSchema.Type?.HasFlag(JsonSchemaType.Object) != true && variantSchema.Properties?.Count is not > 0)
                    Fail($"variant '{mappedName}' must be an object.");

                var variantName = ClassName(mappedName);
                EnsureUniqueName(variantName, schemas);
                if (Variants.ContainsKey(variantName))
                    Fail($"variant '{mappedName}' belongs to more than one base; C# models require a single base.");

                // Shared properties are inherited. A second definition could narrow their type,
                // enum or nullability; refusing that ambiguity is safer than dropping constraints.
                if (schema.Properties != null && variantSchema.Properties != null)
                {
                    var redeclared = schema.Properties.Keys.FirstOrDefault(x =>
                        x != info.Discriminator && variantSchema.Properties.ContainsKey(x));
                    if (redeclared != null)
                        Fail($"variant '{mappedName}' redeclares inherited property '{redeclared}'. Declare it only on the base.");
                }

                ValidateTag(variantSchema, info.Discriminator, mapping.Key, component.Key);
                info.Variants.Add(mapping.Key, variantName);
                Variants.Add(variantName, new OpenApiPolymorphicVariant(info, mapping.Key));
            }

            if (!branchNames.SetEquals(mappedBranches))
                Fail("the discriminator mapping must cover every union variant exactly once.");

            Bases.Add(baseName, info);
        }
    }

    static void ValidateTag(IOpenApiSchema schema, string propertyName, string value, string owner)
    {
        if (schema.Properties == null || !schema.Properties.TryGetValue(propertyName, out var tag))
            return;

        if (tag.Type != JsonSchemaType.String ||
            (tag.Enum?.Count > 0 && (tag.Enum.Count != 1 ||
                tag.Enum[0]?.GetValueKind() != System.Text.Json.JsonValueKind.String || tag.Enum[0]!.GetValue<string>() != value)) ||
            (tag.Const != null && tag.Const != value))
            throw new OpenApiPolymorphismException($"Discriminator on '{owner}': variant property '{propertyName}' must be a string compatible with mapping '{value}'.");
    }

    static string LocalName(IOpenApiSchema schema, IDictionary<string, IOpenApiSchema> schemas, string owner)
    {
        if (schema is not OpenApiSchemaReference reference ||
            !String.IsNullOrEmpty(reference.Reference.ExternalResource) ||
            String.IsNullOrEmpty(reference.Reference.Id) ||
            !schemas.ContainsKey(reference.Reference.Id!))
            throw new OpenApiPolymorphismException($"Discriminator on '{owner}': only resolvable local component $refs are supported; inline or external variants cannot be generated safely.");

        return reference.Reference.Id!;
    }

    static void EnsureUniqueName(string className, IDictionary<string, IOpenApiSchema> schemas)
    {
        if (schemas.Keys.Count(x => ClassName(x) == className) != 1)
            throw new OpenApiPolymorphismException($"Polymorphic component name '{className}' collides with another generated C# name.");
    }

    internal static string ClassName(string name) => name.Pascalize().ToSafeIdentifier();

    internal static void RejectInlineDiscriminator(IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchemaReference && schema.Discriminator != null)
            throw new OpenApiPolymorphismException("Inline discriminated unions are not supported. Put the base in components/schemas and reference it with $ref.");
    }
}
