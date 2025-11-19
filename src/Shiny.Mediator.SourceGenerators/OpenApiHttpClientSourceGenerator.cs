using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class OpenApiHttpClientSourceGenerator : IIncrementalGenerator
{
    static readonly HttpClient HttpClient = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get the root namespace from MSBuild
        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNs);
                provider.GlobalOptions.TryGetValue("build_property.ShinyMediatorHttpNamespace", out var httpNs);
                provider.GlobalOptions.TryGetValue("build_property.AssemblyName", out var assemblyName);
                
                return httpNs ?? rootNs ?? assemblyName ?? "Generated";
            });

        // Find all MediatorHttp items in the project
        var mediatorHttpItems = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(pair =>
            {
                var (text, configOptions) = pair;
                var options = configOptions.GetOptions(text);

                if (!options.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var value))
                    return false;

                return value.Equals("MediatorHttp", StringComparison.InvariantCultureIgnoreCase);
            })
            .Select((pair, _) => pair.Left)
            .Collect();

        var combined = mediatorHttpItems
            .Combine(rootNamespace)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, (sourceContext, data) =>
        {
            var (((texts, defaultNamespace), configOptions), compilation) = data;

            if (texts.IsEmpty)
                return;

            var allHandlers = new List<HandlerRegistrationInfo>();
            string? registrationNamespace = null;

            foreach (var item in texts)
            {
                try
                {
                    var config = GetConfig(configOptions, item, defaultNamespace);
                    
                    // Track the first namespace for registration
                    if (registrationNamespace == null)
                    {
                        registrationNamespace = config.Namespace;
                    }
                    
                    var handlers = ProcessOpenApiDocument(sourceContext, item, config, configOptions, compilation);
                    allHandlers.AddRange(handlers);
                }
                catch (Exception ex)
                {
                    ReportDiagnostic(
                        sourceContext,
                        "SHINYMED001",
                        "Error Generating HTTP Handlers from OpenAPI",
                        $"Error processing {item.Path}: {ex.Message}",
                        DiagnosticSeverity.Error
                    );
                }
            }

            // Generate registration file if we have handlers
            if (allHandlers.Count > 0)
            {
                var registrationCode = HttpHandlerCodeGenerator.GenerateRegistration(
                    allHandlers,
                    registrationNamespace ?? defaultNamespace
                );
                sourceContext.AddSource(
                    "__ShinyHttpClientRegistration.g.cs",
                    SourceText.From(registrationCode, Encoding.UTF8)
                );
            }
        });
    }

    static MediatorHttpItemConfig GetConfig(
        AnalyzerConfigOptionsProvider configProvider,
        AdditionalText item,
        string defaultNamespace)
    {
        var options = configProvider.GetOptions(item);

        return new MediatorHttpItemConfig
        {
            Namespace = GetProperty(options, "Namespace") ?? defaultNamespace,
            ContractPrefix = GetProperty(options, "ContractPrefix"),
            ContractPostfix = GetProperty(options, "ContractPostfix"),
            GenerateModelsOnly = GetProperty(options, "GenerateModelsOnly")?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false,
            UseInternalClasses = GetProperty(options, "UseInternalClasses")?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false,
            GenerateJsonConverters = GetProperty(options, "GenerateJsonConverters")?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false
        };

        static string? GetProperty(AnalyzerConfigOptions options, string propertyName)
        {
            return options.TryGetValue($"build_metadata.AdditionalFiles.{propertyName}", out var value) 
                ? value 
                : null;
        }
    }

    static List<HandlerRegistrationInfo> ProcessOpenApiDocument(
        SourceProductionContext context,
        AdditionalText item,
        MediatorHttpItemConfig config,
        AnalyzerConfigOptionsProvider configProvider,
        Compilation compilation
    )
    {
        var handlers = new List<HandlerRegistrationInfo>();
        
        // Check if this is a remote URI
        var options = configProvider.GetOptions(item);
        var uriString = options.TryGetValue("build_metadata.AdditionalFiles.Uri", out var uri) ? uri : null;

        Stream stream;
        if (!string.IsNullOrWhiteSpace(uriString))
        {
            // Load from remote URL
            try
            {
                stream = HttpClient.GetStreamAsync(new Uri(uriString)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ReportDiagnostic(
                    context,
                    "SHINYMED002",
                    "Error Loading OpenAPI Document from URL",
                    $"Failed to load OpenAPI document from {uriString}: {ex.Message}",
                    DiagnosticSeverity.Error
                );
                return handlers;
            }
        }
        else
        {
            // Load from local file
            var text = item.GetText(context.CancellationToken);
            if (text == null)
            {
                ReportDiagnostic(
                    context,
                    "SHINYMED003",
                    "Error Loading OpenAPI Document",
                    $"Could not load content from {item.Path}",
                    DiagnosticSeverity.Error
                );
                return handlers;
            }

            stream = new MemoryStream(Encoding.UTF8.GetBytes(text.ToString()));
        }

        using (stream)
        {
            // Parse OpenAPI document
            OpenApiDocument document;
            
            try
            {
                // Read the stream content into a byte array
                byte[] bytes;
                using (var memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);
                    bytes = memStream.ToArray();
                }
                
                // Try to parse as OpenAPI document
                using (var docStream = new MemoryStream(bytes))
                {
                    var settings = new OpenApiReaderSettings();
                    settings.AddJsonReader();
                    settings.AddYamlReader();
                    
                    var result = OpenApiDocument.Load(docStream, null, settings);
                    document = result.Document!;
                    
                    if (result.Diagnostic?.Errors.Count > 0)
                    {
                        foreach (var error in result.Diagnostic.Errors)
                        {
                            ReportDiagnostic(
                                context,
                                "SHINYMED004",
                                "OpenAPI Parsing Warning",
                                error.Message,
                                DiagnosticSeverity.Warning
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportDiagnostic(
                    context,
                    "SHINYMED004",
                    "OpenAPI Parsing Error",
                    $"Failed to parse OpenAPI document: {ex.Message}",
                    DiagnosticSeverity.Error
                );
                return handlers;
            }

            // Generate models from components
            GenerateComponents(context, document, config, compilation);

            // Generate handlers from paths (unless GenerateModelsOnly is true)
            if (!config.GenerateModelsOnly)
            {
                handlers = GenerateHandlers(context, document, config, compilation);
            }
        }

        return handlers;
    }

    static void GenerateComponents(
        SourceProductionContext context,
        OpenApiDocument document,
        MediatorHttpItemConfig config,
        Compilation compilation
    )
    {
        if (document.Components?.Schemas == null)
            return;

        var generator = new OpenApiModelGenerator(config, context, compilation);
        
        foreach (var schema in document.Components.Schemas)
        {
            generator.GenerateModel(schema.Key, schema.Value);
        }
    }

    
    static List<HandlerRegistrationInfo> GenerateHandlers(
        SourceProductionContext context,
        OpenApiDocument document,
        MediatorHttpItemConfig config,
        Compilation compilation
    )
    {
        var handlers = new List<HandlerRegistrationInfo>();

        if (document.Paths.Count == 0)
            return handlers;

        foreach (var path in document.Paths)
        {
            if (path.Value?.Operations != null)
            {
                foreach (var operation in path.Value.Operations)
                {
                    // TODO: operation.summary comment for generation
                    // TODO: operationId is null - use HttpVerb+Endpoint to generate a name
                    if (operation.Value == null || string.IsNullOrWhiteSpace(operation.Value.OperationId))
                    {
                        ReportDiagnostic(
                            context,
                            "SHINYMED006",
                            "Missing Operation ID",
                            $"Operation {operation.Key} at path {path.Key} is missing an OperationId",
                            DiagnosticSeverity.Warning
                        );
                    }
                    else
                    {
                        // TODO: operation can never be "unknown" - if it isn't set - we get can build it from the endpoint
                        var handler = GenerateHandlerForOperation(
                            context,
                            path.Key,
                            operation.Key.ToString(),
                            operation.Value,
                            config,
                            compilation,
                            document
                        );
                        handlers.Add(handler);
                    }
                }
            }
        }

        return handlers;
    }

    static HandlerRegistrationInfo GenerateHandlerForOperation(
        SourceProductionContext context,
        string path,
        string operationType,
        OpenApiOperation operation,
        MediatorHttpItemConfig config,
        Compilation compilation,
        OpenApiDocument document
    )
    {
        var contractName = $"{config.ContractPrefix ?? ""}{operation.OperationId!.Pascalize()}{config.ContractPostfix ?? ""}";
        var handlerName = $"{contractName}Handler";

        // Determine response type
        var responseType = GetResponseType(operation, config, document, context);
        
        // Determine HTTP method
        var httpMethod = operationType.ToUpper();

        // Extract properties from parameters and request body
        var properties = new List<HttpPropertyInfo>();
        
        // Process parameters
        if (operation.Parameters != null)
        {
            foreach (var parameter in operation.Parameters)
            {
                if (!String.IsNullOrWhiteSpace(parameter?.Name))
                {
                    var paramType = parameter.In switch
                    {
                        ParameterLocation.Path => HttpParameterType.Path,
                        ParameterLocation.Query => HttpParameterType.Query,
                        ParameterLocation.Header => HttpParameterType.Header,
                        _ => HttpParameterType.Query
                    };

                    // TODO: this is calculating wrong quite often
                    // parameter.Required
                    // parameter.Description for comments
                    var propertyType = GetSchemaType(parameter.Schema!, config, document);

                    // TODO: error on parameter.Name null
                    properties.Add(new HttpPropertyInfo(
                        parameter.Name!.Pascalize(),
                        parameter.Name!,
                        paramType,
                        propertyType
                    ));
                }
            }
        }

        // Process request body
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) ?? false)
        {
            var bodyType = GetSchemaType(mediaType.Schema!, config, document);
            properties.Add(new HttpPropertyInfo(
                "Body",
                "Body",
                HttpParameterType.Body,
                bodyType
            ));
        }

        // Generate contract class
        GenerateContractClass(context, contractName, responseType, properties, config, compilation);

        // Determine if this is a stream request (for now, assume non-stream)
        var isStreamRequest = false;
        var implementsSse = false;

        // Generate handler
        var requestTypeFull = $"global::{config.Namespace}.{contractName}";
        var resultTypeFull = responseType;
        var handlerTypeFull = $"global::{config.Namespace}.{handlerName}";

        var handlerCode = HttpHandlerCodeGenerator.GenerateHandler(
            handlerName,
            requestTypeFull,
            resultTypeFull,
            isStreamRequest,
            httpMethod,
            path,
            properties,
            implementsSse,
            config.Namespace
        );

        var handlerFileName = GetFileName(config.Namespace, handlerName);
        context.AddSource(handlerFileName, SourceText.From(handlerCode, Encoding.UTF8));

        return new HandlerRegistrationInfo(
            handlerTypeFull,
            requestTypeFull,
            resultTypeFull,
            isStreamRequest
        );
    }
    

    static void GenerateContractClass(
        SourceProductionContext context,
        string className,
        string responseType,
        List<HttpPropertyInfo> properties,
        MediatorHttpItemConfig config,
        Compilation compilation
    )
    {
        var accessor = config.UseInternalClasses ? "internal" : "public";
        
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator OpenAPI Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"{accessor} partial class {className} : global::Shiny.Mediator.IRequest<{responseType}>");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            var propType = prop.PropertyType ?? "object";
            sb.AppendLine($"    public {propType} {prop.PropertyName} {{ get; set; }}");
        }

        sb.AppendLine("}");

        var contractSource = sb.ToString();
        var fileName = GetFileName(config.Namespace, className);
        context.AddSource(fileName, SourceText.From(contractSource, Encoding.UTF8));

        // Generate JSON converter if GenerateJsonConverters is true and there are Body parameters
        if (config.GenerateJsonConverters && properties.Any(p => p.ParameterType == HttpParameterType.Body))
        {
            try
            {
                var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions ?? CSharpParseOptions.Default;
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    contractSource, 
                    parseOptions,
                    cancellationToken: context.CancellationToken
                );
                compilation = compilation.AddSyntaxTrees(syntaxTree);
                
                var fullyQualifiedTypeName = $"{config.Namespace}.{className}";
                var typeSymbol = compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
                if (typeSymbol == null)
                {
                    ReportDiagnostic(
                        context, 
                        "SHINYMED007",
                        "Missing Type for JSON Converter",
                        $"Missing Type '{fullyQualifiedTypeName}' for JSON Converter", 
                        DiagnosticSeverity.Warning
                    );
                }
                else
                {
                    JsonConverterSourceGenerator.GenerateJsonConverter(context, typeSymbol);
                }
            }
            catch (Exception ex)
            {
                ReportDiagnostic(
                    context, 
                    "SHINYMED008",
                    "Error in Parsing Generated Code for JSON Converter",
                    $"Error in Parsing Generated Code for JSON Converter: {ex.Message}", 
                    DiagnosticSeverity.Warning
                );
            }
        }
    }
    

    static readonly string[] SuccessResponses = ["200", "201", "202", "203"];
    const string JsonMediaType = "application/json";
    const string FallbackType = "global::System.Net.Http.HttpResponseMessage";
    
    static string GetResponseType(OpenApiOperation operation, MediatorHttpItemConfig config, OpenApiDocument document, SourceProductionContext context)
    {
        var responseType = FallbackType;
        if (operation.Responses == null || operation.Responses.Count == 0)
            return responseType;

        // Try to find a successful response (2xx status codes)
        foreach (var statusCode in SuccessResponses)
        {
            if (operation.Responses.TryGetValue(statusCode, out var response))
            {
                // Check if response has content
                if (response?.Content is { Count: > 0 })
                {
                    // Try application/json first
                    if (response.Content.TryGetValue(JsonMediaType, out var mediaType) && mediaType?.Schema != null)
                    {
                        responseType = GetSchemaType(mediaType.Schema, config, document);
                        // if (mediaType?.Schema != null)
                        // {
                        //     // Cast to OpenApiSchema (same pattern as used for components)
                        //     if (mediaType.Schema is OpenApiSchemaReference { Target: not null } openApiSchema)
                        //     {
                        //         var schemaType = GetSchemaType(openApiSchema.Target!, config, document);
                        //         if (!String.IsNullOrEmpty(schemaType))
                        //         {
                        //             responseType = schemaType;
                        //             break;
                        //         }
                        //     }
                        //     
                        //     ReportDiagnostic(
                        //         context,
                        //         "SHINYMED009",
                        //         "Missing Schema in Response",
                        //         $"Operation '{operation.OperationId}' has {statusCode} response with application/json content but schema could not be resolved. Schema type: {mediaType.Schema.GetType().FullName}",
                        //         DiagnosticSeverity.Warning
                        //     );
                        // }
                        // else
                        // {
                        //     ReportDiagnostic(
                        //         context,
                        //         "SHINYMED009",
                        //         "Missing Schema in Response",
                        //         $"Operation '{operation.OperationId}' has {statusCode} response with application/json content but schema is null",
                        //         DiagnosticSeverity.Warning
                        //     );
                        // }
                    }
                }
            }
        }

        return responseType;
    }
    
    
    static string GetSchemaType(IOpenApiSchema schema, MediatorHttpItemConfig config, OpenApiDocument document)
    {
        // Resolve schema reference if present
        string? schemaType = null;

        // // Check if this schema has a title (meaning it's a named type reference)
        // // In OpenAPI v3.0, when a schema is referenced from components, it typically has a title
        // if (!string.IsNullOrEmpty(schema.Title))
        // {
        //     return $"global::{config.Namespace}.{schema.Title}";
        // }

        // Handle composed schemas by taking the first resolved type (best effort)
        if (schema is OpenApiSchemaReference schemaRef)
        {
            // Extract the schema name from the reference (e.g., "#/components/schemas/Pet" -> "Pet")
            var refId = schemaRef.Reference.ReferenceV3?.Split('/').LastOrDefault();
            schemaType = $"global::{config.Namespace}.{refId}";
        }
        else if (schema.Type!.Value.HasFlag(JsonSchemaType.Object))
        {
            if (schema.AllOf is { Count: > 0 })
            {
                var first = schema.AllOf.OfType<OpenApiSchema>().FirstOrDefault();
                if (first != null)
                    schemaType = GetSchemaType(first, config, document);
            }
            else if (schema.OneOf is { Count: > 0 })
            {
                var first = schema.OneOf.OfType<OpenApiSchema>().FirstOrDefault();
                if (first != null)
                    schemaType = GetSchemaType(first, config, document);
            }
            else if (schema.AnyOf is { Count: > 0 })
            {
                var first = schema.AnyOf.OfType<OpenApiSchema>().FirstOrDefault();
                if (first != null)
                    schemaType = GetSchemaType(first, config, document);
            }
            else
            {
                schemaType = "SHIT";
            }
            // Try to find this schema in the components/schemas by object reference or properties match
            // else if (document.Components?.Schemas != null)
            // {
            //     foreach (var componentSchema in document.Components.Schemas)
            //     {
            //         if (componentSchema.Value != null)
            //         {
            //             // Check if it's the same object reference
            //             if (ReferenceEquals(componentSchema.Value, schema))
            //             {
            //                 schemaType = $"global::{config.Namespace}.{componentSchema.Key}";
            //             }
            //
            //             // Check if schemas match by comparing key properties
            //             // This helps when OpenAPI library creates multiple instances for the same schema
            //             else if (SchemasMatch(componentSchema.Value, schema))
            //             {
            //                 schemaType = $"global::{config.Namespace}.{componentSchema.Key}";
            //             }
            //         }
            //     }
            // }
        }
        else if (schema.Type.Value.HasFlag(JsonSchemaType.String))
            schemaType = GetStringSchemaType(schema);
            
        else if (schema.Type.Value.HasFlag(JsonSchemaType.Integer))
            schemaType = schema.Format == "int64" ? "long" : "int";
        
        else if (schema.Type.Value.HasFlag(JsonSchemaType.Number))
            schemaType = schema.Format == "float" ? "float" : "double";
        
        else if (schema.Type.Value.HasFlag(JsonSchemaType.Boolean))
            schemaType =  "bool";
        
        else if (schema.Type.Value.HasFlag(JsonSchemaType.Array))
        {
            if (schema.Items != null)
                schemaType = $"global::System.Collections.Generic.List<{GetSchemaType(schema.Items, config, document)}>";
        }

        return schemaType!;
    }

    // static bool SchemasMatch(IOpenApiSchema schema1, IOpenApiSchema schema2)
    // {
    //     // If both have properties, they must match exactly
    //     if (schema1.Properties is { Count: > 0 }  && schema2.Properties is { Count: 0 })
    //     {
    //         if (schema1.Properties.Count != schema2.Properties.Count)
    //             return false;
    //         
    //         // Check if all property names match
    //         var props1 = schema1.Properties.Keys.OrderBy(k => k).ToList();
    //         var props2 = schema2.Properties.Keys.OrderBy(k => k).ToList();
    //         
    //         return props1.SequenceEqual(props2);
    //     }
    //     
    //     // If one has properties and the other doesn't, they don't match
    //     var hasProps1 = schema1.Properties is { Count: > 0 };
    //     var hasProps2 = schema2.Properties is { Count: > 0 };
    //     if (hasProps1 != hasProps2)
    //         return false;
    //     
    //     // For schemas without properties, be more strict
    //     // Only match if type, format, and required fields are the same
    //     if (schema1.Type != schema2.Type)
    //         return false;
    //     
    //     if (schema1.Format != schema2.Format)
    //         return false;
    //     
    //     // Check if both have the same required fields
    //     var required1 = schema1.Required ?? new HashSet<string>();
    //     var required2 = schema2.Required ?? new HashSet<string>();
    //     
    //     if (required1.Count != required2.Count)
    //         return false;
    //     
    //     if (!required1.OrderBy(x => x).SequenceEqual(required2.OrderBy(x => x)))
    //         return false;
    //     
    //     return true;
    // }

    
    static string GetStringSchemaType(IOpenApiSchema schema) => schema.Format switch
    {
        "date-time" => "global::System.DateTimeOffset",
        "uuid" => "global::System.Guid",
        "date" => "global::System.DateOnly",
        "time" => "global::System.TimeOnly",
        _ => "string"
    };
    

    static void ReportDiagnostic(
        SourceProductionContext context,
        string id,
        string title,
        string message,
        DiagnosticSeverity severity
    )
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                title,
                "{0}",
                "ShinyMediator.OpenApi",
                severity,
                true,
                message
            ),
            Location.None,
            message
        ));
    }

    
    static string GetFileName(string namespaceName, string typeName)
    {
        // Sanitize namespace to be file-system safe
        var sanitizedNamespace = namespaceName.Replace("<", "_").Replace(">", "_");
        return $"{sanitizedNamespace}.{typeName}.g.cs";
    }
}

