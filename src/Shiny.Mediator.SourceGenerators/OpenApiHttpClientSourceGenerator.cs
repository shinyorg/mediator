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
                    var handler = GenerateHandlerForOperation(
                        context,
                        path.Key,
                        operation.Key.ToString(),
                        operation.Value,
                        config,
                        compilation
                    );
                    handlers.Add(handler);
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
        Compilation compilation
    )
    {
        var opId = operation.OperationId?.Pascalize() ?? $"{operationType.Pascalize()}{path.Split('/').Last().Pascalize()}";
        var contractName = $"{config.ContractPrefix ?? ""}{opId}{config.ContractPostfix ?? ""}";
        var handlerName = $"{contractName}Handler";

        // Determine response type
        var responseType = GetResponseType(operation, config);
        
        // Determine HTTP method
        var httpMethod = operationType.ToUpper();

        // Extract properties from parameters and request body
        var properties = new List<HttpPropertyInfo>();
        
        // Process parameters
        if (operation.Parameters != null)
        {
            foreach (var parameter in operation.Parameters)
            {
                if (!String.IsNullOrWhiteSpace(parameter!.Name))
                {
                    var paramType = parameter.In switch
                    {
                        ParameterLocation.Path => HttpParameterType.Path,
                        ParameterLocation.Query => HttpParameterType.Query,
                        ParameterLocation.Header => HttpParameterType.Header,
                        _ => HttpParameterType.Query
                    };

                    var propertyType = GetSchemaType(parameter.Schema!, config);
                    properties.Add(new HttpPropertyInfo(
                        parameter.Name!.Pascalize(),
                        parameter.Name!,
                        parameter.Required,
                        paramType,
                        propertyType,
                        parameter.Description
                    ));
                }
            }
        }

        // Process request body
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) ?? false)
        {
            var bodyType = GetSchemaType(mediaType.Schema!, config);
            properties.Add(new HttpPropertyInfo(
                "Body",
                "Body",
                operation.RequestBody.Required,
                HttpParameterType.Body,
                bodyType,
                operation.RequestBody.Description
            ));
        }

        // Generate contract class
        GenerateContractClass(context, contractName, responseType, operation.Summary, properties, config, compilation);

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
        string? comment,
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
        sb.AppendLine();
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);

        if (!String.IsNullOrWhiteSpace(comment))
        {
            sb.AppendLine( "/// <summary>");
            sb.AppendLine($"/// {comment}");
            sb.AppendLine( "/// </summary>");
        }
        
        sb.AppendLine($"{accessor} partial class {className} : global::Shiny.Mediator.IRequest<{responseType}>");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            if (!String.IsNullOrWhiteSpace(prop.Comments))
            {
                sb.AppendLine( "    /// <summary>");
                sb.AppendLine($"    /// {prop.Comments}");
                sb.AppendLine( "    /// </summary>");
            }

            var required = prop.IsRequired ? "required " : String.Empty;
            var nullable = prop.IsRequired ? String.Empty : "?";
            sb.AppendLine($"    public {required}{prop.PropertyType}{nullable} {prop.PropertyName} {{ get; set; }}");
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
    
    static string GetResponseType(OpenApiOperation operation, MediatorHttpItemConfig config)
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
                        responseType = GetSchemaType(mediaType.Schema, config);
                }
            }
        }

        return responseType;
    }
    
    
    static string GetSchemaType(IOpenApiSchema schema, MediatorHttpItemConfig config)
    {
        string? schemaType = null;
        
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
                    schemaType = GetSchemaType(first, config);
            }
            else if (schema.OneOf is { Count: > 0 })
            {
                var first = schema.OneOf.OfType<OpenApiSchema>().FirstOrDefault();
                if (first != null)
                    schemaType = GetSchemaType(first, config);
            }
            else if (schema.AnyOf is { Count: > 0 })
            {
                var first = schema.AnyOf.OfType<OpenApiSchema>().FirstOrDefault();
                if (first != null)
                    schemaType = GetSchemaType(first, config);
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
                schemaType = $"global::System.Collections.Generic.List<{GetSchemaType(schema.Items, config)}>";
        }

        if (String.IsNullOrEmpty(schemaType))
            throw new InvalidOperationException("Unable to determine schema type");
        
        return schemaType!;
    }
    
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

