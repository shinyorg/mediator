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
using Microsoft.OpenApi.YamlReader;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class OpenApiHttpClientSourceGenerator : IIncrementalGenerator
{
    private static readonly HttpClient HttpClient = new();

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

            foreach (var item in texts)
            {
                try
                {
                    var config = GetConfig(configOptions, item, defaultNamespace);
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
                    defaultNamespace
                );
                sourceContext.AddSource(
                    "__ShinyHttpClientRegistration.g.cs",
                    SourceText.From(registrationCode, Encoding.UTF8)
                );
            }
        });
    }

    private static MediatorHttpItemConfig GetConfig(
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

    private static List<HandlerRegistrationInfo> ProcessOpenApiDocument(
        SourceProductionContext context,
        AdditionalText item,
        MediatorHttpItemConfig config,
        AnalyzerConfigOptionsProvider configProvider,
        Compilation compilation)
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
                    document = result.Document;
                    
                    if (result.Diagnostic?.Errors?.Count > 0)
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

    private static void GenerateComponents(
        SourceProductionContext context,
        OpenApiDocument document,
        MediatorHttpItemConfig config,
        Compilation compilation)
    {
        if (document.Components?.Schemas == null)
            return;

        var generator = new OpenApiModelGenerator(config, context, compilation);
        
        foreach (var schema in document.Components.Schemas)
        {
            if (schema.Value is OpenApiSchema openApiSchema)
            {
                generator.GenerateModel(schema.Key, openApiSchema);
            }
        }
    }

    private static List<HandlerRegistrationInfo> GenerateHandlers(
        SourceProductionContext context,
        OpenApiDocument document,
        MediatorHttpItemConfig config,
        Compilation compilation)
    {
        var handlers = new List<HandlerRegistrationInfo>();

        if (document.Paths == null || document.Paths.Count == 0)
            return handlers;

        foreach (var path in document.Paths)
        {
            if (path.Value?.Operations == null)
                continue;

            foreach (var operation in path.Value.Operations)
            {
                if (operation.Value == null || string.IsNullOrWhiteSpace(operation.Value.OperationId))
                {
                    ReportDiagnostic(
                        context,
                        "SHINYMED006",
                        "Missing Operation ID",
                        $"Operation {operation.Key} at path {path.Key} is missing an OperationId",
                        DiagnosticSeverity.Warning
                    );
                    continue;
                }

                var handler = GenerateHandlerForOperation(
                    context,
                    path.Key,
                    operation.Key.ToString(),
                    operation.Value,
                    config,
                    compilation,
                    document
                );

                if (handler != null)
                {
                    handlers.Add(handler);
                }
            }
        }

        return handlers;
    }

    private static HandlerRegistrationInfo GenerateHandlerForOperation(
        SourceProductionContext context,
        string path,
        string operationType,
        OpenApiOperation operation,
        MediatorHttpItemConfig config,
        Compilation compilation,
        OpenApiDocument document)
    {
        var contractName = $"{config.ContractPrefix ?? ""}{(operation.OperationId ?? "Unknown").Pascalize()}{config.ContractPostfix ?? ""}";
        var handlerName = $"{contractName}Handler";

        // Determine response type
        var responseType = GetResponseType(operation, config, document);
        
        // Determine HTTP method
        var httpMethod = operationType.ToUpper();

        // Extract properties from parameters and request body
        var properties = new List<HttpPropertyInfo>();
        
        // Process parameters
        if (operation.Parameters != null)
        {
            foreach (var parameter in operation.Parameters)
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.Name))
                    continue;

                var paramType = parameter.In switch
                {
                    ParameterLocation.Path => HttpParameterType.Path,
                    ParameterLocation.Query => HttpParameterType.Query,
                    ParameterLocation.Header => HttpParameterType.Header,
                    _ => HttpParameterType.Query
                };

                var propertyType = parameter.Schema != null 
                    ? GetSchemaType(parameter.Schema as OpenApiSchema ?? new OpenApiSchema(), config, document)
                    : "string";

                properties.Add(new HttpPropertyInfo(
                    (parameter.Name ?? "Unknown").Pascalize(),
                    parameter.Name ?? "Unknown",
                    paramType,
                    propertyType
                ));
            }
        }

        // Process request body
        if (operation.RequestBody?.Content != null && 
            operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            var bodyType = mediaType.Schema != null 
                ? GetSchemaType(mediaType.Schema as OpenApiSchema ?? new OpenApiSchema(), config, document)
                : "object";

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

        context.AddSource($"{handlerName}.g.cs", SourceText.From(handlerCode, Encoding.UTF8));

        return new HandlerRegistrationInfo(
            handlerTypeFull,
            requestTypeFull,
            resultTypeFull,
            isStreamRequest
        );
    }

    private static void GenerateContractClass(
        SourceProductionContext context,
        string className,
        string responseType,
        List<HttpPropertyInfo> properties,
        MediatorHttpItemConfig config,
        Compilation compilation)
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
        context.AddSource($"{className}.g.cs", SourceText.From(contractSource, Encoding.UTF8));

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

    private static string GetResponseType(OpenApiOperation operation, MediatorHttpItemConfig config, OpenApiDocument document)
    {
        if (operation.Responses != null && operation.Responses.TryGetValue("200", out var response200))
        {
            if (response200?.Content != null && 
                response200.Content.TryGetValue("application/json", out var mediaType) &&
                mediaType.Schema is OpenApiSchema schema)
            {
                return GetSchemaType(schema, config, document);
            }
        }

        return "global::System.Net.Http.HttpResponseMessage";
    }

    private static string GetSchemaType(OpenApiSchema schema, MediatorHttpItemConfig config, OpenApiDocument document)
    {
        // Try to find this schema in the components/schemas by object reference
        if (document.Components?.Schemas != null)
        {
            foreach (var componentSchema in document.Components.Schemas)
            {
                if (ReferenceEquals(componentSchema.Value, schema))
                {
                    return $"global::{config.Namespace}.{componentSchema.Key}";
                }
            }
        }

        // Check if this schema has a title (meaning it's a named type reference)
        // In OpenAPI v3.0, when a schema is referenced from components, it typically has a title
        if (!string.IsNullOrEmpty(schema.Title))
        {
            return $"global::{config.Namespace}.{schema.Title}";
        }

        if (schema.Type != null)
        {
            if (schema.Type.Value.HasFlag(JsonSchemaType.String))
                return GetStringSchemaType(schema);
            
            if (schema.Type.Value.HasFlag(JsonSchemaType.Integer))
                return schema.Format == "int64" ? "long" : "int";
            
            if (schema.Type.Value.HasFlag(JsonSchemaType.Number))
                return schema.Format == "float" ? "float" : "double";
            
            if (schema.Type.Value.HasFlag(JsonSchemaType.Boolean))
                return "bool";
            
            if (schema.Type.Value.HasFlag(JsonSchemaType.Array))
            {
                var itemsSchema = schema.Items as OpenApiSchema;
                if (itemsSchema != null)
                    return $"global::System.Collections.Generic.List<{GetSchemaType(itemsSchema, config, document)}>";
            }
        }

        return "object";
    }

    private static string GetStringSchemaType(OpenApiSchema schema)
    {
        return schema.Format switch
        {
            "date-time" => "global::System.DateTimeOffset",
            "uuid" => "global::System.Guid",
            "date" => "global::System.DateOnly",
            "time" => "global::System.TimeOnly",
            _ => "string"
        };
    }

    private static void ReportDiagnostic(
        SourceProductionContext context,
        string id,
        string title,
        string message,
        DiagnosticSeverity severity)
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
}