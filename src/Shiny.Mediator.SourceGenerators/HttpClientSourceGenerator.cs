using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class HttpClientSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get MSBuild configuration options
        var msbuildOptions = context
            .CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select((pair, _) =>
            {
                var (compilation, provider) = pair;
                
                provider.GlobalOptions.TryGetValue("build_property.ShinyMediatorHttpRegistrationClassName", out var className);
                provider.GlobalOptions.TryGetValue("build_property.ShinyMediatorHttpRegistrationMethodName", out var methodName);
                provider.GlobalOptions.TryGetValue("build_property.ShinyMediatorHttpRegistrationAccessModifier", out var useInternalString);
                
                provider.GlobalOptions.TryGetValue("build_property.ShinyMediatorHttpNamespace", out var @namespace);
                if (String.IsNullOrWhiteSpace(@namespace))
                {
                    provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out @namespace);
                    if (String.IsNullOrWhiteSpace(@namespace))
                        @namespace = compilation.AssemblyName;
                }
                
                var useInternal = useInternalString?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? Constants.DefaultHttpRegistrationUseInternal;
                    
                return new HttpMsBuildOptions(
                    useInternal,
                    @namespace!,
                    String.IsNullOrWhiteSpace(className) ? Constants.DefaultHttpRegistrationClassName : className!,
                    String.IsNullOrWhiteSpace(methodName) ? Constants.DefaultHttpRegistrationMethodName : methodName!
                );
            });

        // Find all classes with HTTP attributes (Get, Post, Put, Patch, Delete)
        var httpRequests = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateHttpRequest(s),
                transform: static (ctx, _) => GetHttpRequestResult(ctx)
            )
            .Where(static m => m is not null)
            .Collect();

        // Combine and generate
        var combined = httpRequests.Combine(msbuildOptions);

        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var requests = source.Left;
            var options = source.Right;

            if (!requests.IsEmpty)
                Execute(requests, options, spc);
        });
    }

    static bool IsCandidateHttpRequest(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        return classDecl.AttributeLists.Count > 0;
    }

    
    static HttpRequestResult? GetHttpRequestResult(GeneratorSyntaxContext context)
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
            return new HttpRequestResult(
                RequestInfo: null,
                Diagnostic: new DiagnosticInfo(
                    "SHINYMED_HTTP001",
                    "HTTP request must implement IRequest<> or IStreamRequest<>",
                    $"Type '{classSymbol.Name}' must implement IRequest<TResult> or IStreamRequest<TResult> to use HTTP method attributes",
                    classDecl.Identifier.GetLocation()
                )
            );
        }

        // Get properties with attributes
        var properties = new List<PropertyInfo>();
        var bodyCount = 0;
        
        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var headerAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "HeaderAttribute");
            var queryAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "QueryAttribute");
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
                    AttributeName: headerName
                ));
            }
            else if (queryAttr is not null)
            {
                var queryName = queryAttr.ConstructorArguments.Length > 0 && 
                                queryAttr.ConstructorArguments[0].Value is string qname
                    ? qname
                    : member.Name;

                properties.Add(new PropertyInfo(
                    Name: member.Name,
                    Type: member.Type,
                    PropertyType: PropertyType.Query,
                    AttributeName: queryName
                ));
            }
            else if (bodyAttr is not null)
            {
                bodyCount++;
                if (bodyCount > 1)
                {
                    return new HttpRequestResult(
                        RequestInfo: null,
                        Diagnostic: new DiagnosticInfo(
                            "SHINYMED_HTTP002",
                            "Only one [Body] attribute allowed per request",
                            $"Type '{classSymbol.Name}' has multiple [Body] attributes. Only one body is allowed per HTTP request",
                            classDecl.Identifier.GetLocation()
                        )
                    );
                }
                
                properties.Add(new PropertyInfo(
                    Name: member.Name,
                    Type: member.Type,
                    PropertyType: PropertyType.Body,
                    AttributeName: null
                ));
            }
            else
            {
                // Regular property - might be route parameter
                properties.Add(new PropertyInfo(
                    Name: member.Name,
                    Type: member.Type,
                    PropertyType: PropertyType.RouteOrQuery,
                    AttributeName: null
                ));
            }
        }
        
        // Validate route parameters exist as properties
        var routeParams = ExtractRouteParameters(route);
        foreach (var routeParam in routeParams)
        {
            var propExists = properties.Any(p => p.Name.Equals(routeParam, StringComparison.OrdinalIgnoreCase));
            if (!propExists)
            {
                return new HttpRequestResult(
                    RequestInfo: null,
                    Diagnostic: new DiagnosticInfo(
                        "SHINYMED_HTTP003",
                        "Route parameter must exist as class property",
                        $"Type '{classSymbol.Name}' has route parameter '{routeParam}' in route '{route}', but no matching property was found. Add a property named '{routeParam}' to the class",
                        classDecl.Identifier.GetLocation()
                    )
                );
            }
        }

        // Check if implements IServerSentEventsStream
        var implementsSse = classSymbol.AllInterfaces
            .Any(i => i.Name == "IServerSentEventsStream");

        var requestInfo = new HttpRequestInfo(
            ClassSymbol: classSymbol,
            Route: route,
            HttpMethod: httpMethod,
            IsStreamRequest: isStreamRequest,
            ResultType: resultType,
            Properties: properties.ToImmutableArray(),
            ImplementsServerSentEvents: implementsSse
        );

        return new HttpRequestResult(requestInfo, null);
    }

    static bool IsHttpMethodAttribute(string? name) => name is "GetAttribute" or "PostAttribute" or "PutAttribute" or "PatchAttribute" or "DeleteAttribute";

    static List<string> ExtractRouteParameters(string route)
    {
        var parameters = new List<string>();
        var startIndex = 0;
        
        while (true)
        {
            var openBrace = route.IndexOf('{', startIndex);
            if (openBrace == -1) break;
            
            var closeBrace = route.IndexOf('}', openBrace);
            if (closeBrace == -1) break;
            
            var paramName = route.Substring(openBrace + 1, closeBrace - openBrace - 1);
            parameters.Add(paramName);
            
            startIndex = closeBrace + 1;
        }
        
        return parameters;
    }

    static void Execute(
        ImmutableArray<HttpRequestResult?> results,
        HttpMsBuildOptions options,
        SourceProductionContext context
    )
    {
        if (results.IsEmpty)
            return;

        var validRequests = new List<HttpRequestInfo>();

        // First, report all diagnostics and collect valid requests
        foreach (var result in results)
        {
            if (result is null)
                continue;

            if (result.Diagnostic is not null)
            {
                var diag = result.Diagnostic;
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        diag.Id,
                        diag.Title,
                        diag.Message,
                        "ShinyMediator.Http",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true
                    ),
                    diag.Location
                ));
            }
            else if (result.RequestInfo is not null)
            {
                validRequests.Add(result.RequestInfo);
            }
        }
        
        if (validRequests.Count == 0)
            return;

        var allHandlers = new List<HandlerRegistrationInfo>();

        // Generate individual handlers
        foreach (var request in validRequests)
        {
            var typeName = GetSimpleTypeName(request.ClassSymbol);
            
            var handlerClassName = request.IsStreamRequest ? $"{typeName}StreamHandler" : $"{typeName}Handler";
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

            var handlerFileName = $"{options.Namespace}.{handlerClassName}.g.cs";
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
        var registrationCode = HttpHandlerCodeGenerator.GenerateRegistration(
            allHandlers, 
            options.Namespace,
            options.ClassName,
            options.MethodName,
            options.UseInternalAccessModifier
        );
        
        context.AddSource(options.ClassName + ".g.cs", SourceText.From(registrationCode, Encoding.UTF8));
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
                parameterName = prop.AttributeName ?? prop.Name;
            }
            else if (prop.PropertyType == PropertyType.Query)
            {
                paramType = HttpParameterType.Query;
                parameterName = prop.AttributeName ?? prop.Name;
            }
            else if (prop.PropertyType == PropertyType.Body)
            {
                paramType = HttpParameterType.Body;
                parameterName = prop.Name;
            }
            else // RouteOrQuery
            {
                // Check if property is in route
                var routeParam = $"{{{prop.Name}}}";
                if (route.Contains(routeParam, StringComparison.OrdinalIgnoreCase))
                {
                    paramType = HttpParameterType.Path;
                    parameterName = prop.Name;
                }
                else
                {
                    // Not in route, so it's ignored (not used as query param automatically)
                    continue;
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
    bool UseInternalAccessModifier,
    string Namespace,
    string ClassName,
    string MethodName
);

record HttpRequestResult(
    HttpRequestInfo? RequestInfo,
    DiagnosticInfo? Diagnostic
);

record DiagnosticInfo(
    string Id,
    string Title,
    string Message,
    Location Location
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
    string? AttributeName
);

enum PropertyType
{
    Header,
    Query,
    Body,
    RouteOrQuery
}