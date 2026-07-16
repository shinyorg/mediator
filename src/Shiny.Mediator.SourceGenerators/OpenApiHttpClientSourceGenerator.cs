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
        var rootNamespace = context
            .CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select((pair, _) =>
            {
                var (compilation, provider) = pair;

                var regNamespace = (provider.GlobalOptions.PriorityGetBuildProperty(
                    "ShinyMediatorOpenApiRegistrationNamespace", 
                    "RootNamespace"
                ) ?? compilation.AssemblyName)!;
                
                var regClassName = provider.GlobalOptions.GetBuildProperty(
                    "ShinyMediatorOpenApiRegistrationClassName", 
                    Constants.DefaultOpenApiRegistrationClassName
                )!;
                var regMethodName = provider.GlobalOptions.GetBuildProperty(
                    "ShinyMediatorOpenApiRegistrationMethodName", 
                    Constants.DefaultOpenApiRegistrationMethodName
                )!;
                
                var regUseInternal = provider.GlobalOptions.GetBuildProperty(
                    "ShinyMediatorOpenApiRegistrationUseInternalClass",
                    Constants.DefaultOpenApiRegistrationUseInternal.ToString()
                )!.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                
                return new OpenApiRegistrationConfig(
                    regNamespace,
                    regClassName,
                    regMethodName,
                    regUseInternal
                ); 
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
            var (((texts, regProperties), configOptions), compilation) = data;
            var handlers = new List<HandlerRegistrationInfo>();
            
            if (!texts.IsEmpty)
            {
                foreach (var item in texts)
                {
                    try
                    {
                        var config = GetConfig(configOptions, item, regProperties.RootNamespace);

                        var newHandlers = ProcessOpenApiDocument(sourceContext, item, config, configOptions, compilation);
                        if (newHandlers.Count > 0)
                            handlers.AddRange(newHandlers);
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
            }

            if (handlers.Count > 0)
            {
                var registrationCode = HttpHandlerCodeGenerator.GenerateRegistration(
                    handlers,
                    regProperties.RootNamespace,
                    regProperties.RegistrationClassName,
                    regProperties.RegistrationMethodName,
                    regProperties.UseInternalClass
                );
                
                sourceContext.AddSource(
                    regProperties.RegistrationClassName + ".g.cs",
                    SourceText.From(registrationCode, Encoding.UTF8)
                );
            }
        });
    }

    static MediatorHttpItemConfig GetConfig(
        AnalyzerConfigOptionsProvider configProvider,
        AdditionalText item,
        string defaultNamespace
    )
    {
        var options = configProvider.GetOptions(item);

        return new MediatorHttpItemConfig
        {
            Namespace = GetProperty(options, "Namespace", defaultNamespace)!,
            ContractPrefix = GetProperty(options, "ContractPrefix", null),
            ContractPostfix = GetProperty(options, "ContractPostfix", "HttpRequest"),
            GenerateModelsOnly = GetProperty(options, "GenerateModelsOnly", null)?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false,
            UseInternalClasses = GetProperty(options, "UseInternalClasses", null)?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? Constants.DefaultOpenApiRegistrationUseInternal,
            GenerateJsonConverters = GetProperty(options, "GenerateJsonConverters", null)?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? Constants.DefaultOpenApiGenerateJsonConverters
        };
    }


    static string? GetProperty(AnalyzerConfigOptions options, string propertyName, string? defaultValue)
    {
        if (!options.TryGetValue($"build_metadata.AdditionalFiles.{propertyName}", out var value) || String.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value;
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

            // Create a single model generator to track generated types across components and contracts
            var modelGenerator = new OpenApiModelGenerator(config, context, compilation);

            // Generate models from components FIRST
            GenerateComponents(document, modelGenerator);

            // Generate handlers from paths (unless GenerateModelsOnly is true)
            if (!config.GenerateModelsOnly)
                handlers = GenerateHandlers(context, document, config, modelGenerator);

            // Emit a custom IJsonTypeInfoResolver that wires every generated per-type JsonConverter
            // via JsonMetadataServices.CreateValueInfo + collection-shape wrappers, plus a
            // [ModuleInitializer] that registers it with Shiny.Json. Required for AOT/trim safety —
            // without it Json.Default has no JsonTypeInfo for generated types and serialization throws.
            //
            // We don't emit a partial JsonSerializerContext + [JsonSerializable] because STJ's own
            // source generator doesn't see partial classes emitted by another generator in the same
            // compilation, so the abstract members of JsonSerializerContext would never be filled in.
            if (config.GenerateJsonConverters && (modelGenerator.ConvertedTypeNames.Count > 0 || modelGenerator.EnumTypeNames.Count > 0))
                GenerateJsonResolver(context, config, modelGenerator, handlers);
        }

        return handlers;
    }


    static void GenerateJsonResolver(
        SourceProductionContext context,
        MediatorHttpItemConfig config,
        OpenApiModelGenerator modelGenerator,
        List<HandlerRegistrationInfo> handlers
    )
    {
        var resolverName = DeriveContextName(config.Namespace) + "JsonResolver";
        var initName = resolverName + "Init";

        // Only types that have a generated per-type JsonConverter are candidates — models always do
        // (when GenerateJsonConverters is on); contracts only when they have a Body parameter. The
        // model generator + contract generator both track this on modelGenerator.ConvertedTypeNames
        // (FQNs without the "global::" prefix).
        var converted = modelGenerator.ConvertedTypeNames;
        var enums = modelGenerator.EnumTypeNames;
        if (converted.Count == 0 && enums.Count == 0)
            return;

        // Sort for deterministic output.
        var ordered = converted.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var orderedEnums = enums.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator OpenAPI Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization.Metadata;");
        sb.AppendLine();
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"internal sealed class {resolverName} : IJsonTypeInfoResolver");
        sb.AppendLine("{");
        sb.AppendLine($"    internal static readonly {resolverName} Instance = new();");
        sb.AppendLine();
        sb.AppendLine("    public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)");
        sb.AppendLine("    {");

        // Per-type: class element TypeInfo backed by the source-generated JsonConverter.
        foreach (var fqn in ordered)
        {
            var typeRef = $"global::{fqn}";
            var converterRef = $"global::{fqn}JsonConverter";
            sb.AppendLine($"        if (type == typeof({typeRef}))");
            sb.AppendLine($"            return JsonMetadataServices.CreateValueInfo<{typeRef}>(options, new {converterRef}());");
        }

        // Per-enum: register the enum and its Nullable<T> counterpart with the right converter.
        // String enums use System.Text.Json.Serialization.JsonStringEnumConverter<T> (a factory —
        // CreateValueInfo accepts factories directly but our NullableEnumConverter<T> needs an
        // unwrapped JsonConverter<T>, so we materialize via factory.CreateConverter for the
        // nullable case). Integer enums use the converter returned by
        // JsonMetadataServices.GetEnumConverter<T>, which is already JsonConverter<T>.
        foreach (var kv in orderedEnums)
        {
            var typeRef = $"global::{kv.Key}";

            if (kv.Value)
            {
                // String enum: materialize the JsonStringEnumConverter<T> factory into the concrete
                // JsonConverter<T> at resolution time. CreateValueInfo + factories silently produce
                // unsupported-type JsonTypeInfo; the unwrapped converter is what STJ actually needs.
                sb.AppendLine($"        if (type == typeof({typeRef}))");
                sb.AppendLine("        {");
                sb.AppendLine($"            var __f = new global::System.Text.Json.Serialization.JsonStringEnumConverter<{typeRef}>();");
                sb.AppendLine($"            var __inner = (global::System.Text.Json.Serialization.JsonConverter<{typeRef}>)__f.CreateConverter(typeof({typeRef}), options);");
                sb.AppendLine($"            return JsonMetadataServices.CreateValueInfo<{typeRef}>(options, __inner);");
                sb.AppendLine("        }");
                sb.AppendLine($"        if (type == typeof({typeRef}?))");
                sb.AppendLine("        {");
                sb.AppendLine($"            var __f = new global::System.Text.Json.Serialization.JsonStringEnumConverter<{typeRef}>();");
                sb.AppendLine($"            var __inner = (global::System.Text.Json.Serialization.JsonConverter<{typeRef}>)__f.CreateConverter(typeof({typeRef}), options);");
                sb.AppendLine($"            return JsonMetadataServices.CreateValueInfo<{typeRef}?>(options, new NullableEnumConverter<{typeRef}>(__inner));");
                sb.AppendLine("        }");
            }
            else
            {
                // Integer enum: GetEnumConverter<T> already returns JsonConverter<T>, no factory dance.
                sb.AppendLine($"        if (type == typeof({typeRef}))");
                sb.AppendLine($"            return JsonMetadataServices.CreateValueInfo<{typeRef}>(options, JsonMetadataServices.GetEnumConverter<{typeRef}>(options));");
                sb.AppendLine($"        if (type == typeof({typeRef}?))");
                sb.AppendLine($"            return JsonMetadataServices.CreateValueInfo<{typeRef}?>(options, new NullableEnumConverter<{typeRef}>(JsonMetadataServices.GetEnumConverter<{typeRef}>(options)));");
            }
        }

        // Per-type: collection wrappers (List<T>, T[]) using metadata services factories. Element
        // TypeInfo is looked up via this same resolver — they compose cleanly inside one assembly.
        foreach (var fqn in ordered)
        {
            var typeRef = $"global::{fqn}";
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.List<{typeRef}>))");
            sb.AppendLine($"            return CreateList<{typeRef}>(options);");
            sb.AppendLine($"        if (type == typeof({typeRef}[]))");
            sb.AppendLine($"            return CreateArray<{typeRef}>(options);");
        }

        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateList<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options);");
        sb.AppendLine("        if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateListInfo<List<T>, T>(");
        sb.AppendLine("            options,");
        sb.AppendLine("            new JsonCollectionInfoValues<List<T>>");
        sb.AppendLine("            {");
        sb.AppendLine("                ObjectCreator = static () => new List<T>(),");
        sb.AppendLine("                ElementInfo = elem");
        sb.AppendLine("            });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateArray<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options);");
        sb.AppendLine("        if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateArrayInfo<T>(");
        sb.AppendLine("            options,");
        sb.AppendLine("            new JsonCollectionInfoValues<T[]>");
        sb.AppendLine("            {");
        sb.AppendLine("                ElementInfo = elem");
        sb.AppendLine("            });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo<T> ResolveElement<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        try { return options.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>; }");
        sb.AppendLine("        catch (System.InvalidOperationException) { return null; }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    sealed class NullableEnumConverter<T> : global::System.Text.Json.Serialization.JsonConverter<T?> where T : struct, System.Enum");
        sb.AppendLine("    {");
        sb.AppendLine("        readonly global::System.Text.Json.Serialization.JsonConverter<T> inner;");
        sb.AppendLine("        public NullableEnumConverter(global::System.Text.Json.Serialization.JsonConverter<T> inner) => this.inner = inner;");
        sb.AppendLine();
        sb.AppendLine("        public override T? Read(ref global::System.Text.Json.Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == global::System.Text.Json.JsonTokenType.Null) return null;");
        sb.AppendLine("            return this.inner.Read(ref reader, typeof(T), options);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public override void Write(global::System.Text.Json.Utf8JsonWriter writer, T? value, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value is null) { writer.WriteNullValue(); return; }");
        sb.AppendLine("            this.inner.Write(writer, value.Value, options);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"internal static class {initName}");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine($"    internal static void Init() => global::Shiny.Json.AddResolver({resolverName}.Instance);");
        sb.AppendLine("}");

        context.AddSource(GetFileName(config.Namespace, resolverName), SourceText.From(sb.ToString(), Encoding.UTF8));
    }


    static string DeriveContextName(string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
            return "OpenApi";

        var lastDot = namespaceName.LastIndexOf('.');
        return lastDot < 0 ? namespaceName : namespaceName.Substring(lastDot + 1);
    }

    static void GenerateComponents(
        OpenApiDocument document,
        OpenApiModelGenerator modelGenerator
    )
    {
        if (document.Components?.Schemas == null)
            return;
        
        foreach (var schema in document.Components.Schemas)
        {
            modelGenerator.GenerateModel(schema.Key, schema.Value);
        }
    }

    
    static List<HandlerRegistrationInfo> GenerateHandlers(
        SourceProductionContext context,
        OpenApiDocument document,
        MediatorHttpItemConfig config,
        OpenApiModelGenerator modelGenerator
    )
    {
        var handlers = new List<HandlerRegistrationInfo>();

        if (document.Paths.Count == 0)
            return handlers;

        // Tracks the contract names already emitted for this document so operations that derive the
        // same name — e.g. two operationId-less paths that differ only by path parameters
        // (/entity/{id}/schedule vs /entity/{id}/schedule/{year}/{month}) — get disambiguated instead
        // of colliding on an AddSource hintName (which throws and aborts the whole document).
        var usedContractNames = new HashSet<string>(StringComparer.Ordinal);

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
                        modelGenerator,
                        usedContractNames
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
        OpenApiModelGenerator modelGenerator,
        HashSet<string> usedContractNames
    )
    {
        var opId = (operation.OperationId?.Pascalize() ?? $"{operationType.Pascalize()}{String.Concat(path.Split('/').Where(s => !String.IsNullOrWhiteSpace(s) && !s.Contains("{")).Select(s => s.Pascalize()))}").ToSafeIdentifier();
        var contractName = $"{config.ContractPrefix ?? ""}{opId}{config.ContractPostfix ?? ""}".ToSafeIdentifier();

        // Disambiguate names that collide (operationId-less paths differing only by path parameters).
        // First try appending the trailing path parameters (…/schedule/{year}/{month} -> "ByYearMonth"),
        // then fall back to a numeric suffix — keeping every emitted type name unique and deterministic.
        if (usedContractNames.Contains(contractName))
        {
            var suffix = String.Concat(GetTrailingPathParameterNames(path).Select(p => p.Pascalize()));
            if (!String.IsNullOrEmpty(suffix))
            {
                opId = $"{opId}By{suffix}".ToSafeIdentifier();
                contractName = $"{config.ContractPrefix ?? ""}{opId}{config.ContractPostfix ?? ""}".ToSafeIdentifier();
            }

            var baseOpId = opId;
            var baseContractName = contractName;
            var counter = 2;
            while (usedContractNames.Contains(contractName))
            {
                opId = $"{baseOpId}{counter}";
                contractName = $"{baseContractName}{counter}";
                counter++;
            }
        }
        usedContractNames.Add(contractName);

        var handlerName = $"{contractName}Handler";

        // Determine response type
        var responseType = GetResponseType(operation, config, modelGenerator, opId);
        
        // Determine HTTP method
        var httpMethod = operationType.ToUpper();

        // Extract properties from parameters and request body
        var properties = new List<HttpPropertyInfo>();
        
        // Extract path parameters from the path string that might not be in operation.Parameters
        var pathParameterNames = ExtractPathParameterNames(path);
        var explicitPathParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Process explicit parameters
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

                    if (paramType == HttpParameterType.Path)
                        explicitPathParamNames.Add(parameter.Name!);

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
        
        // Add any path parameters from the path string that weren't explicitly defined
        foreach (var pathParamName in pathParameterNames)
        {
            if (!explicitPathParamNames.Contains(pathParamName))
            {
                properties.Add(new HttpPropertyInfo(
                    pathParamName.Pascalize(),
                    pathParamName,
                    true, // Path parameters are always required
                    HttpParameterType.Path,
                    "string", // Default to string type when not explicitly defined
                    null
                ));
            }
        }

        // Process request body
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) ?? false)
        {
            var bodyType = ResolveSchemaTypeOrSynthesize(mediaType.Schema!, config, modelGenerator, $"{opId}Body");
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
        GenerateContractClass(context, contractName, responseType, operation.Summary, properties, config, modelGenerator);

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
        OpenApiModelGenerator modelGenerator
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

            var nullable = prop.IsRequired ? String.Empty : "?";
            sb.AppendLine($"    public {prop.PropertyType}{nullable} {prop.PropertyName} {{ get; set; }}");
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
                var compilation = modelGenerator.Compilation;
                var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions ?? CSharpParseOptions.Default;
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    contractSource, 
                    parseOptions,
                    cancellationToken: context.CancellationToken
                );
                compilation = compilation.AddSyntaxTrees(syntaxTree);
                modelGenerator.UpdateCompilation(compilation);
                
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
                    modelGenerator.ConvertedTypeNames.Add(fullyQualifiedTypeName);
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
    
    static string GetResponseType(
        OpenApiOperation operation,
        MediatorHttpItemConfig config,
        OpenApiModelGenerator modelGenerator,
        string opId
    )
    {
        var responseType = FallbackType;
        if (operation.Responses == null || operation.Responses.Count == 0)
            return responseType;

        // Try to find a successful response (2xx status codes) - SuccessResponses is ordered, so the
        // lowest declared 2xx wins. Without stopping at the first match, an operation declaring both
        // (say) 200 and 202 would take the *last* one, and ResolveSchemaTypeOrSynthesize would also
        // synthesize "{opId}Response" twice for inline schemas.
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
                        responseType = ResolveSchemaTypeOrSynthesize(mediaType.Schema, config, modelGenerator, $"{opId}Response");
                        break;
                    }
                }
            }
        }

        return responseType;
    }


    // Resolves the CLR type for a response/body schema. When the schema is an inline (anonymous)
    // object or an array of inline objects — i.e. the spec doesn't $ref a named component — we
    // synthesize a named model from it via the model generator (which also emits its JSON converter
    // and wires it into the resolver) rather than collapsing it to a weakly-typed "object". Anything
    // else ($ref, scalars, dictionaries) falls through to the regular scalar resolver.
    static string ResolveSchemaTypeOrSynthesize(
        IOpenApiSchema schema,
        MediatorHttpItemConfig config,
        OpenApiModelGenerator modelGenerator,
        string synthesizedName
    )
    {
        // $ref → already points at a named, component-generated type.
        if (schema is OpenApiSchemaReference)
            return GetSchemaType(schema, config);

        // Inline array → synthesize the element type (if it too is inline) and wrap in List<T>.
        if (schema.Type?.HasFlag(JsonSchemaType.Array) == true && schema.Items != null)
        {
            var elementType = ResolveSchemaTypeOrSynthesize(schema.Items, config, modelGenerator, $"{synthesizedName}Item");
            return $"global::System.Collections.Generic.List<{elementType}>";
        }

        // Inline object with real properties (and not a dictionary / composed schema) → synthesize
        // a named model so the caller gets a strongly-typed contract instead of "object".
        if (schema.Type?.HasFlag(JsonSchemaType.Object) == true
            && schema.Properties is { Count: > 0 }
            && schema.AdditionalProperties == null
            && !(schema.AllOf?.Count > 0)
            && !(schema.OneOf?.Count > 0)
            && !(schema.AnyOf?.Count > 0))
        {
            modelGenerator.GenerateModel(synthesizedName, schema);
            var typeName = synthesizedName.Pascalize().ToSafeIdentifier();
            return $"global::{config.Namespace}.{typeName}";
        }

        // Scalars, dictionaries, composed schemas, bare objects → existing resolution.
        return GetSchemaType(schema, config);
    }

    
    static string GetSchemaType(IOpenApiSchema schema, MediatorHttpItemConfig config)
    {
        string? schemaType = null;
        
        if (schema is OpenApiSchemaReference schemaRef)
        {
            // Extract the schema name from the reference (e.g., "#/components/schemas/Pet" -> "Pet")
            var refId = schemaRef.Reference.ReferenceV3?.Split('/').LastOrDefault()?.Pascalize().ToSafeIdentifier();
            schemaType = $"global::{config.Namespace}.{refId}";
        }
        // Prioritize format over type - when format indicates a numeric type, use it
        // This handles cases like "type": ["integer", "string"] with "format": "int32"
        else if (IsNumericFormat(schema.Format))
        {
            schemaType = schema.Format == "int64" ? "long" : 
                         schema.Format == "float" ? "float" : 
                         schema.Format == "double" ? "double" : "int";
        }
        else if (schema.Type?.HasFlag(JsonSchemaType.Object) == true)
        {
            if (schema.AllOf is { Count: > 0 })
            {
                var first = schema.AllOf.OfType<OpenApiSchema>().FirstOrDefault();
                if (first != null)
                    schemaType = GetSchemaType(first, config);
            }
            else if (schema.OneOf is { Count: > 0 })
            {
                // Filter out null types and get the first non-null schema
                var nonNullSchema = schema.OneOf
                    .Where(s => s is OpenApiSchemaReference || (s.Type?.HasFlag(JsonSchemaType.Null) != true))
                    .FirstOrDefault();
                if (nonNullSchema != null)
                    schemaType = GetSchemaType(nonNullSchema, config);
            }
            else if (schema.AnyOf is { Count: > 0 })
            {
                // Filter out null types and get the first non-null schema
                var nonNullSchema = schema.AnyOf
                    .Where(s => s is OpenApiSchemaReference || (s.Type?.HasFlag(JsonSchemaType.Null) != true))
                    .FirstOrDefault();
                if (nonNullSchema != null)
                    schemaType = GetSchemaType(nonNullSchema, config);
            }
            else if (schema.AdditionalProperties != null)
            {
                // Dictionary type - object with additionalProperties
                var valueType = GetSchemaType(schema.AdditionalProperties, config);
                schemaType = $"global::System.Collections.Generic.Dictionary<string, {valueType}>";
            }
            else
            {
                // Inline object without a reference - fall back to object
                schemaType = "object";
            }
        }
        // Handle oneOf/anyOf at top level (without explicit object type)
        else if (schema.OneOf is { Count: > 0 })
        {
            // Filter out null types and get the first non-null schema
            var nonNullSchema = schema.OneOf
                .Where(s => s is OpenApiSchemaReference || (s.Type?.HasFlag(JsonSchemaType.Null) != true))
                .FirstOrDefault();
            if (nonNullSchema != null)
                schemaType = GetSchemaType(nonNullSchema, config);
        }
        else if (schema.AnyOf is { Count: > 0 })
        {
            // Filter out null types and get the first non-null schema
            var nonNullSchema = schema.AnyOf
                .Where(s => s is OpenApiSchemaReference || (s.Type?.HasFlag(JsonSchemaType.Null) != true))
                .FirstOrDefault();
            if (nonNullSchema != null)
                schemaType = GetSchemaType(nonNullSchema, config);
        }
        else if (schema.AllOf is { Count: > 0 })
        {
            var first = schema.AllOf.OfType<OpenApiSchema>().FirstOrDefault();
            if (first != null)
                schemaType = GetSchemaType(first, config);
        }
        else if (schema.Type?.HasFlag(JsonSchemaType.Integer) == true)
            schemaType = schema.Format == "int64" ? "long" : "int";

        else if (schema.Type?.HasFlag(JsonSchemaType.Number) == true)
            schemaType = schema.Format == "float" ? "float" : "double";

        else if (schema.Type?.HasFlag(JsonSchemaType.Boolean) == true)
            schemaType =  "bool";

        else if (schema.Type?.HasFlag(JsonSchemaType.Array) == true)
        {
            if (schema.Items != null)
                schemaType = $"global::System.Collections.Generic.List<{GetSchemaType(schema.Items, config)}>";
        }

        else if (schema.Type?.HasFlag(JsonSchemaType.String) == true)
            schemaType = GetStringSchemaType(schema);
        
        // Fallback for unknown or null types
        if (String.IsNullOrEmpty(schemaType))
            schemaType = "object";
        
        return schemaType!;
    }
    
    static bool IsNumericFormat(string? format)
        => format is "int32" or "int64" or "float" or "double";
    
    static string GetStringSchemaType(IOpenApiSchema schema) => schema.Format switch
    {
        "date-time" => "global::System.DateTimeOffset",
        "uuid" => "global::System.Guid",
        "date" => "global::System.DateOnly",
        "time" => "global::System.TimeOnly",
        _ => "string"
    };
    
    
    // Path parameter names that trail the last literal segment, e.g. "/entity/{id}/schedule/{year}/{month}"
    // -> [year, month]. Used to disambiguate collisions between operationId-less paths.
    static IEnumerable<string> GetTrailingPathParameterNames(string path)
    {
        var segments = path.Split('/').Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
        var lastLiteral = -1;
        for (var i = 0; i < segments.Count; i++)
        {
            if (!segments[i].Contains("{"))
                lastLiteral = i;
        }

        var result = new List<string>();
        for (var i = lastLiteral + 1; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.StartsWith("{") && seg.EndsWith("}") && seg.Length > 2)
                result.Add(seg.Substring(1, seg.Length - 2));
        }
        return result;
    }


    static List<string> ExtractPathParameterNames(string path)
    {
        var parameters = new List<string>();
        var startIndex = 0;
        
        while ((startIndex = path.IndexOf('{', startIndex)) != -1)
        {
            var endIndex = path.IndexOf('}', startIndex);
            if (endIndex == -1)
                break;
                
            var paramName = path.Substring(startIndex + 1, endIndex - startIndex - 1);
            if (!String.IsNullOrWhiteSpace(paramName))
                parameters.Add(paramName);
                
            startIndex = endIndex + 1;
        }
        
        return parameters;
    }
    

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

record OpenApiRegistrationConfig(
    string RootNamespace,
    string RegistrationClassName,
    string RegistrationMethodName,
    bool UseInternalClass
);