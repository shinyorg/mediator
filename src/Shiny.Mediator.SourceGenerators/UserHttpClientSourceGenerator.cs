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

        var allHandlers = new List<HandlerRegistrationInfo>();

        // Generate individual handlers
        foreach (var request in validRequests)
        {
            var handlerClassName = $"{GetSimpleTypeName(request!.ClassSymbol)}HttpHandler";
            var requestTypeFull = GetFullTypeName(request.ClassSymbol);
            var resultTypeFull = GetFullTypeName(request.ResultType);
            var handlerTypeFull = $"global::{options.Namespace}.{handlerClassName}";

            // Convert properties to HttpPropertyInfo
            var httpProperties = ConvertToHttpPropertyInfo(request.Properties, request.Route);

            // Generate handler using HttpHandlerCodeGenerator
            var handlerCode = HttpHandlerCodeGenerator.GenerateHandler(
                handlerClassName,
                requestTypeFull,
                resultTypeFull,
                request.IsStreamRequest,
                request.HttpMethod,
                request.Route,
                httpProperties,
                request.ImplementsServerSentEvents,
                options.Namespace
            );

            var handlerFileName = $"{GetSimpleTypeName(request.ClassSymbol)}_HttpHandler.g.cs";
            context.AddSource(handlerFileName, SourceText.From(handlerCode, Encoding.UTF8));

            // Add to registration list
            allHandlers.Add(new HandlerRegistrationInfo(
                handlerTypeFull,
                requestTypeFull,
                resultTypeFull,
                request.IsStreamRequest
            ));
        }

        // Generate registration extension method using HttpHandlerCodeGenerator
        var registrationCode = HttpHandlerCodeGenerator.GenerateRegistration(allHandlers, options.Namespace);
        context.AddSource("__ShinyHttpClientRegistration.g.cs", SourceText.From(registrationCode, Encoding.UTF8));
    }

    static List<HttpPropertyInfo> ConvertToHttpPropertyInfo(ImmutableArray<PropertyInfo> properties, string route)
    {
        var httpProperties = new List<HttpPropertyInfo>();

        foreach (var prop in properties)
        {
            HttpParameterType paramType;
            string parameterName;

            if (prop.PropertyType == PropertyType.Header)
            {
                paramType = HttpParameterType.Header;
                parameterName = prop.HeaderName ?? prop.Name;
            }
            else if (prop.PropertyType == PropertyType.Body)
            {
                paramType = HttpParameterType.Body;
                parameterName = prop.Name;
            }
            else // RouteOrQuery
            {
                // Check if property is in route or query string
                var routeParam = $"{{{prop.Name}}}";
                if (route.Contains(routeParam))
                {
                    paramType = HttpParameterType.Path;
                    parameterName = prop.Name;
                }
                else
                {
                    paramType = HttpParameterType.Query;
                    parameterName = prop.Name;
                }
            }

            httpProperties.Add(new HttpPropertyInfo(
                prop.Name,
                parameterName,
                false,
                paramType,
                GetFullTypeName(prop.Type)
            ));
        }

        return httpProperties;
    }

    static string GetFullTypeName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    static string GetSimpleTypeName(ITypeSymbol type) => type.Name;
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