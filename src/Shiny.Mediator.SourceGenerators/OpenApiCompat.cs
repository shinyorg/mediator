using System;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Shiny.Mediator.SourceGenerators;


/// <summary>
/// Keeps generated output stable across Microsoft.OpenApi reader behavior changes (3.9+).
/// </summary>
static class OpenApiCompat
{
    public const string NullableExtension = "x-shiny-nullable";

    static readonly Regex JsonNullable = new(
        @"(?<!\\)""nullable""\s*:\s*true",
        RegexOptions.Compiled
    );
    static readonly Regex YamlBlockNullable = new(
        @"^(?<indent>[ \t]*(?:-[ \t]+)?)[""']?nullable[""']?[ \t]*:[ \t]*true[ \t]*(?<comment>#[^\r\n]*)?(?<cr>\r?)$",
        RegexOptions.Compiled | RegexOptions.Multiline
    );
    static readonly Regex YamlFlowNullable = new(
        @"(?<pre>[{,][ \t]*)[""']?nullable[""']?[ \t]*:[ \t]*true(?=[ \t]*[,}])",
        RegexOptions.Compiled
    );


    /// <summary>
    /// Microsoft.OpenApi 3.9+ discards OpenAPI 3.0 <c>nullable: true</c> on a schema without a <c>type</c>
    /// (https://github.com/microsoft/OpenAPI.NET/issues/2967) - the <c>nullable: true</c> + <c>allOf: [$ref]</c>
    /// shape Swashbuckle and NSwag emit for nullable references. This marks every <c>nullable: true</c> with an
    /// <see cref="NullableExtension"/> extension (which the reader keeps) before the document is parsed.
    /// Works on the raw text so YAML aliases are never expanded here.
    /// </summary>
    public static string TagNullable(string document)
    {
        var trimmed = document.TrimStart('﻿', ' ', '\t', '\r', '\n');
        if (trimmed.StartsWith("{"))
            return JsonNullable.Replace(document, $"\"nullable\": true, \"{NullableExtension}\": true");

        var result = YamlBlockNullable.Replace(document, m =>
        {
            var indent = m.Groups["indent"].Value;
            var siblingIndent = indent.Replace('-', ' ');
            var cr = m.Groups["cr"].Value;
            var comment = m.Groups["comment"].Success ? " " + m.Groups["comment"].Value : String.Empty;
            return $"{indent}nullable: true{comment}{cr}\n{siblingIndent}{NullableExtension}: true{cr}";
        });
        return YamlFlowNullable.Replace(result, m => $"{m.Groups["pre"].Value}nullable: true, {NullableExtension}: true");
    }


    /// <summary>
    /// The schema allows null - through its type (3.1 <c>type: [x, "null"]</c>, or 3.0 <c>nullable</c> next to a
    /// <c>type</c>) or through a type-less 3.0 <c>nullable: true</c> tagged by <see cref="TagNullable"/>.
    /// </summary>
    public static bool IsNullable(IOpenApiSchema schema)
        => schema.Type?.HasFlag(JsonSchemaType.Null) == true
           || schema.Extensions?.ContainsKey(NullableExtension) == true;


    /// <summary>
    /// Microsoft.OpenApi 3.9+ folds a <c>oneOf</c>/<c>anyOf</c> of plain types (e.g. string | number | object) into
    /// one <see cref="JsonSchemaType"/> with several flags. Earlier readers kept the branches and the first one -
    /// string, in practice - won. True when several non-null types are set and string is one of them.
    /// </summary>
    public static bool IsMixedTypeWithString(IOpenApiSchema schema)
    {
        if (schema.Type == null)
            return false;

        var nonNull = schema.Type.Value & ~JsonSchemaType.Null;
        return nonNull.HasFlag(JsonSchemaType.String) && nonNull != JsonSchemaType.String;
    }
}
