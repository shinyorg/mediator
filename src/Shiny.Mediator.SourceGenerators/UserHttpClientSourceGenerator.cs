using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class UserHttpClientSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get MSBuild configuration options
        var msbuildOptions = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select((pair, _) =>
            {
                var (compilation, provider) = pair;
                
                provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                provider.GlobalOptions.TryGetValue("build_property.ShinyMediatorHttpNamespace", out var httpNamespace);
                
                var assemblyName = compilation.AssemblyName ?? "Generated";
                var targetNamespace = httpNamespace ?? rootNamespace ?? assemblyName;
                
                return new HttpMsBuildOptions(
                    Namespace: targetNamespace,
                    AssemblyName: assemblyName
                );
            });

        // Find all classes with HTTP attributes (Get, Post, Put, Patch, Delete)
        var httpRequests = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateHttpRequest(s),
                transform: static (ctx, _) => GetHttpRequestInfo(ctx)
            )
            .Where(static m => m is not null)
            .Collect();

        // Combine and generate
        var combined = httpRequests.Combine(msbuildOptions);

        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var requests = source.Left;
            var options = source.Right;

            if (requests.IsEmpty)
                return;

            Execute(requests, options, spc);
        });
    }

    static bool IsCandidateHttpRequest(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        return classDecl.AttributeLists.Count > 0;
    }

    static HttpRequestInfo? GetHttpRequestInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        
        if (symbol is not INamedTypeSymbol classSymbol)
            return null;

        // Check for HTTP method attributes
        var httpAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => IsHttpMethodAttribute(a.AttributeClass?.Name));

        if (httpAttribute is null)
            return null;

        // Get route from attribute
        var route = httpAttribute.ConstructorArguments.Length > 0
            ? httpAttribute.ConstructorArguments[0].Value?.ToString()
            : null;

        if (route is null)
            return null;

        // Determine HTTP method from attribute name
        var httpMethod = httpAttribute.AttributeClass!.Name switch
        {
            "GetAttribute" => "Get",
            "PostAttribute" => "Post",
            "PutAttribute" => "Put",
            "PatchAttribute" => "Patch",
            "DeleteAttribute" => "Delete",
            _ => null
        };

        if (httpMethod is null)
            return null;

        // Check if implements IRequest<> or IStreamRequest<>
        var isStreamRequest = false;
        ITypeSymbol? resultType = null;

        foreach (var iface in classSymbol.AllInterfaces)
        {
            if (iface is { Name: "IRequest", TypeArguments.Length: 1 })
            {
                resultType = iface.TypeArguments[0];
                isStreamRequest = false;
                break;
            }
            if (iface is { Name: "IStreamRequest", TypeArguments.Length: 1 })
            {
                resultType = iface.TypeArguments[0];
                isStreamRequest = true;
                break;
            }
        }

        if (resultType is null)
        {
            // TODO: Report diagnostic - must implement IRequest<> or IStreamRequest<>
            return null;
        }

        // Get properties with attributes
        var properties = new List<PropertyInfo>();
        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var headerAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "HeaderAttribute");
            var bodyAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "BodyAttribute");

            if (headerAttr is not null)
            {
                var headerName = headerAttr.ConstructorArguments.Length > 0 && 
                                headerAttr.ConstructorArguments[0].Value is string name
                    ? name
                    : member.Name;

                properties.Add(new PropertyInfo(
                    Name: member.Name,
                    Type: member.Type,
                    PropertyType: PropertyType.Header,
                    HeaderName: headerName
                ));
            }
            else if (bodyAttr is not null)
            {
                properties.Add(new PropertyInfo(
                    Name: member.Name,
                    Type: member.Type,
                    PropertyType: PropertyType.Body,
                    HeaderName: null
                ));
            }
            else
            {
                // Regular property - might be route or query parameter
                properties.Add(new PropertyInfo(
                    Name: member.Name,
                    Type: member.Type,
                    PropertyType: PropertyType.RouteOrQuery,
                    HeaderName: null
                ));
            }
        }

        // Check if implements IServerSentEventsStream
        var implementsSse = classSymbol.AllInterfaces
            .Any(i => i.Name == "IServerSentEventsStream");

        return new HttpRequestInfo(
            ClassSymbol: classSymbol,
            Route: route,
            HttpMethod: httpMethod,
            IsStreamRequest: isStreamRequest,
            ResultType: resultType,
            Properties: properties.ToImmutableArray(),
            ImplementsServerSentEvents: implementsSse
        );
    }

    static bool IsHttpMethodAttribute(string? name)
    {
        return name is "GetAttribute" or "PostAttribute" or "PutAttribute" or "PatchAttribute" or "DeleteAttribute";
    }

    static void Execute(
        ImmutableArray<HttpRequestInfo?> requests,
        HttpMsBuildOptions options,
        SourceProductionContext context
    )
    {
        var validRequests = requests.Where(r => r is not null).ToList();
        
        if (validRequests.Count == 0)
            return;

        // Generate individual handlers
        foreach (var request in validRequests)
        {
            var handlerCode = GenerateHandler(request!, options);
            var handlerFileName = $"{GetSimpleTypeName(request!.ClassSymbol)}_HttpHandler.g.cs";
            context.AddSource(handlerFileName, SourceText.From(handlerCode, Encoding.UTF8));
        }

        // Generate registration extension method
        var registrationCode = GenerateRegistration(validRequests, options);
        context.AddSource("__ShinyHttpClientRegistration.g.cs", SourceText.From(registrationCode, Encoding.UTF8));
    }

    static string GenerateHandler(HttpRequestInfo request, HttpMsBuildOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator HTTP Client Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        
        if (request.IsStreamRequest)
        {
            sb.AppendLine("using System.Collections.Generic;");
        }
        
        sb.AppendLine();
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);

        var requestTypeFull = GetFullTypeName(request.ClassSymbol);
        var resultTypeFull = GetFullTypeName(request.ResultType);
        var handlerClassName = $"{GetSimpleTypeName(request.ClassSymbol)}HttpHandler";

        if (request.IsStreamRequest)
        {
            sb.AppendLine($"public partial class {handlerClassName}(global::Shiny.Mediator.Http.HttpHandlerServices services)");
            sb.AppendLine($"    : global::Shiny.Mediator.Http.BaseHttpRequestHandler(services),");
            sb.AppendLine($"      global::Shiny.Mediator.IStreamRequestHandler<{requestTypeFull}, {resultTypeFull}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public global::System.Collections.Generic.IAsyncEnumerable<{resultTypeFull}> Handle(");
            sb.AppendLine($"        {requestTypeFull} request,");
            sb.AppendLine($"        global::Shiny.Mediator.IMediatorContext context,");
            sb.AppendLine($"        global::System.Threading.CancellationToken cancellationToken)");
            sb.AppendLine("    {");
        }
        else
        {
            sb.AppendLine($"public partial class {handlerClassName}(global::Shiny.Mediator.Http.HttpHandlerServices services)");
            sb.AppendLine($"    : global::Shiny.Mediator.Http.BaseHttpRequestHandler(services),");
            sb.AppendLine($"      global::Shiny.Mediator.IRequestHandler<{requestTypeFull}, {resultTypeFull}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public global::System.Threading.Tasks.Task<{resultTypeFull}> Handle(");
            sb.AppendLine($"        {requestTypeFull} request,");
            sb.AppendLine($"        global::Shiny.Mediator.IMediatorContext context,");
            sb.AppendLine($"        global::System.Threading.CancellationToken cancellationToken)");
            sb.AppendLine("    {");
        }

        // Build the route with parameters
        sb.Append($"        var route = $\"{ProcessRoute(request.Route, request.Properties)}\"");
        sb.AppendLine(";");
        
        // Create HTTP request message
        var httpMethodMapping = request.HttpMethod switch
        {
            "Get" => "global::System.Net.Http.HttpMethod.Get",
            "Post" => "global::System.Net.Http.HttpMethod.Post",
            "Put" => "global::System.Net.Http.HttpMethod.Put",
            "Patch" => "global::System.Net.Http.HttpMethod.Patch",
            "Delete" => "global::System.Net.Http.HttpMethod.Delete",
            _ => "global::System.Net.Http.HttpMethod.Get"
        };

        sb.AppendLine($"        var httpRequest = new global::System.Net.Http.HttpRequestMessage({httpMethodMapping}, route);");
        sb.AppendLine();

        // Add headers
        var headerProps = request.Properties.Where(p => p.PropertyType == PropertyType.Header).ToList();
        foreach (var prop in headerProps)
        {
            sb.AppendLine($"        if (request.{prop.Name} != null)");
            sb.AppendLine($"            httpRequest.Headers.Add(\"{prop.HeaderName}\", request.{prop.Name}.ToString());");
            sb.AppendLine();
        }

        // Add body
        var bodyProp = request.Properties.FirstOrDefault(p => p.PropertyType == PropertyType.Body);
        if (bodyProp is not null)
        {
            sb.AppendLine($"        if (request.{bodyProp.Name} != null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var json = services.Serializer.Serialize(request.{bodyProp.Name});");
            sb.AppendLine("            httpRequest.Content = new global::System.Net.Http.StringContent(json, global::System.Text.Encoding.UTF8, \"application/json\");");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Call base handler method
        if (request.IsStreamRequest)
        {
            var useSse = request.ImplementsServerSentEvents ? "true" : $"request is global::Shiny.Mediator.Http.IServerSentEventsStream";
            sb.AppendLine($"        return this.HandleStream<{requestTypeFull}, {resultTypeFull}>(");
            sb.AppendLine("            httpRequest,");
            sb.AppendLine("            request,");
            sb.AppendLine($"            {useSse},");
            sb.AppendLine("            context,");
            sb.AppendLine("            cancellationToken");
            sb.AppendLine("        );");
        }
        else
        {
            sb.AppendLine($"        return this.HandleRequest<{requestTypeFull}, {resultTypeFull}>(");
            sb.AppendLine("            httpRequest,");
            sb.AppendLine("            request,");
            sb.AppendLine("            context,");
            sb.AppendLine("            cancellationToken");
            sb.AppendLine("        );");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    static string ProcessRoute(string route, ImmutableArray<PropertyInfo> properties)
    {
        // Replace route parameters and query string parameters with interpolated values
        var result = route;
        
        foreach (var prop in properties)
        {
            if (prop.PropertyType != PropertyType.RouteOrQuery)
                continue;

            // Check if property is in route or query string
            var routeParam = $"{{{prop.Name}}}";
            if (result.Contains(routeParam))
            {
                // Route parameter - use direct interpolation
                result = result.Replace(routeParam, $"{{request.{prop.Name}}}");
            }
            else
            {
                // Query parameter - URL encode
                var queryParam = $"={{{prop.Name}}}";
                if (result.Contains(queryParam))
                {
                    result = result.Replace(queryParam, $"={{global::System.Uri.EscapeDataString(request.{prop.Name}?.ToString() ?? \"\")}}");
                }
            }
        }
        
        return result;
    }

    static string GenerateRegistration(List<HttpRequestInfo?> requests, HttpMsBuildOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator HTTP Client Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
        sb.AppendLine();
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine("public static class __ShinyHttpClientRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddHttpHandlers(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var request in requests)
        {
            if (request is null)
                continue;

            var requestTypeFull = GetFullTypeName(request.ClassSymbol);
            var resultTypeFull = GetFullTypeName(request.ResultType);
            var handlerClassName = $"{GetSimpleTypeName(request.ClassSymbol)}HttpHandler";
            var handlerTypeFull = $"{options.Namespace}.{handlerClassName}";

            if (request.IsStreamRequest)
            {
                sb.AppendLine($"        services.TryAddSingleton<global::Shiny.Mediator.IStreamRequestHandler<{requestTypeFull}, {resultTypeFull}>, {handlerTypeFull}>();");
            }
            else
            {
                sb.AppendLine($"        services.TryAddSingleton<global::Shiny.Mediator.IRequestHandler<{requestTypeFull}, {resultTypeFull}>, {handlerTypeFull}>();");
            }
        }

        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    static string GetFullTypeName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    static string GetSimpleTypeName(ITypeSymbol type)
    {
        return type.Name;
    }
}

record HttpMsBuildOptions(
    string Namespace,
    string AssemblyName
);

record HttpRequestInfo(
    INamedTypeSymbol ClassSymbol,
    string Route,
    string HttpMethod,
    bool IsStreamRequest,
    ITypeSymbol ResultType,
    ImmutableArray<PropertyInfo> Properties,
    bool ImplementsServerSentEvents
);

record PropertyInfo(
    string Name,
    ITypeSymbol Type,
    PropertyType PropertyType,
    string? HeaderName
);

enum PropertyType
{
    Header,
    Body,
    RouteOrQuery
}