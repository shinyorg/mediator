using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shiny.Mediator.AspNet.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class MediatorEndpointSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Define the pipeline to find classes with MediatorHttp attributes
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null);

        // Combine with compilation to access symbols
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the output
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol is null)
            return null;

        // Check if this class has any MediatorHttp attributes
        var httpAttributes = classSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name?.Contains("MediatorHttp") == true)
            .ToList();

        if (!httpAttributes.Any())
            return null;

        // Check if this class implements IRequestHandler or ICommandHandler
        var isRequestHandler = classSymbol.AllInterfaces.Any(i => 
            i.IsGenericType && 
            i.OriginalDefinition.ToDisplayString() == "Shiny.Mediator.IRequestHandler<TRequest, TResult>");

        var isCommandHandler = classSymbol.AllInterfaces.Any(i => 
            i.IsGenericType && 
            i.OriginalDefinition.ToDisplayString() == "Shiny.Mediator.ICommandHandler<TCommand>");

    
        if (!isRequestHandler && !isCommandHandler)
            return null;

        return new ClassInfo(
            classSymbol.ToDisplayString(),
            classSymbol.Name,
            httpAttributes.Select(attr => GetAttributeInfo(attr)).ToList(),
            isRequestHandler,
            isCommandHandler,
            GetGenericTypes(classSymbol)
        );
    }

    static AttributeInfo GetAttributeInfo(AttributeData attribute)
    {
        var operationId = attribute.ConstructorArguments.Length > 0 
            ? attribute.ConstructorArguments[0].Value?.ToString() ?? ""
            : "";
        
        var uriTemplate = attribute.ConstructorArguments.Length > 1 
            ? attribute.ConstructorArguments[1].Value?.ToString() ?? ""
            : "";

        var httpMethod = GetHttpMethod(attribute.AttributeClass?.Name ?? "");

        var properties = new Dictionary<string, object>();
        foreach (var namedArg in attribute.NamedArguments)
        {
            properties[namedArg.Key] = namedArg.Value.Value ?? "";
        }

        return new AttributeInfo(operationId, uriTemplate, httpMethod, properties);
    }

    static string GetHttpMethod(string attributeName) => attributeName switch
    {
        "MediatorHttpGetAttribute" => "Get",
        "MediatorHttpPostAttribute" => "Post",
        "MediatorHttpPutAttribute" => "Put",
        "MediatorHttpDeleteAttribute" => "Delete",
        _ => "Post"
    };

    static GenericTypeInfo GetGenericTypes(INamedTypeSymbol classSymbol)
    {
        var requestInterface = classSymbol.AllInterfaces.FirstOrDefault(i => 
            i.IsGenericType && 
            i.OriginalDefinition.ToDisplayString() == "Shiny.Mediator.IRequestHandler<TRequest, TResult>");

        var commandInterface = classSymbol.AllInterfaces.FirstOrDefault(i => 
            i.IsGenericType && 
            i.OriginalDefinition.ToDisplayString() == "Shiny.Mediator.ICommandHandler<TCommand>");

        string requestType = "";
        string resultType = "";

        if (requestInterface != null)
        {
            requestType = requestInterface.TypeArguments[0].ToDisplayString();
            resultType = requestInterface.TypeArguments[1].ToDisplayString();
        }
        else if (commandInterface != null)
        {
            requestType = commandInterface.TypeArguments[0].ToDisplayString();
            resultType = "void";
        }

        return new GenericTypeInfo(requestType, resultType);
    }

    static void Execute(Compilation compilation, ImmutableArray<ClassInfo?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var validClasses = classes.Where(c => c != null).Cast<ClassInfo>().ToList();
        if (!validClasses.Any())
            return;

        var nameSpace = compilation.AssemblyName ?? "Generated";
        
        // Generate dependency injection extensions
        GenerateDependencyInjectionExtensions(context, nameSpace, validClasses);
        
        // Generate endpoint mapping extensions
        GenerateEndpointMappingExtensions(context, nameSpace, validClasses);
    }

    static void GenerateDependencyInjectionExtensions(SourceProductionContext context, string nameSpace, List<ClassInfo> classes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator ASP.NET Source Generator.");
        sb.AppendLine("// Changes may cause incorrect behavior and will be lost if the code is");
        sb.AppendLine("// regenerated.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine($"namespace {nameSpace};");
        sb.AppendLine();
        sb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"Shiny.Mediator.AspNet\", \"1.0.0\")]");
        sb.AppendLine("public static class MediatorDependencyInjectionExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers all generated mediator endpoint handlers");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static global::Shiny.Mediator.ShinyMediatorBuilder AddGeneratedEndpoints(this global::Shiny.Mediator.ShinyMediatorBuilder builder)");
        sb.AppendLine("    {");

        foreach (var classInfo in classes)
        {
            sb.AppendLine($"        builder.Services.AddScopedAsImplementedInterfaces<{classInfo.FullName}>();");
        }

        sb.AppendLine("        return builder;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("MediatorDependencyInjectionExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static void GenerateEndpointMappingExtensions(SourceProductionContext context, string nameSpace, List<ClassInfo> classes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator ASP.NET Source Generator.");
        sb.AppendLine("// Changes may cause incorrect behavior and will be lost if the code is");
        sb.AppendLine("// regenerated.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine($"namespace {nameSpace};");
        sb.AppendLine();
        sb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"Shiny.Mediator.AspNet\", \"1.0.0\")]");
        sb.AppendLine("public static class MediatorEndpoints");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Maps all generated mediator endpoints");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapGeneratedMediatorEndpoints(this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder)");
        sb.AppendLine("    {");

        foreach (var classInfo in classes)
        {
            foreach (var attribute in classInfo.HttpAttributes)
            {
                GenerateEndpointMapping(sb, classInfo, attribute);
            }
        }

        sb.AppendLine("        return builder;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("MediatorEndpoints.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static void GenerateEndpointMapping(StringBuilder sb, ClassInfo classInfo, AttributeInfo attribute)
    {
        var httpMethod = attribute.HttpMethod.ToLower();
        var operationId = attribute.OperationId;
        var uriTemplate = attribute.UriTemplate;
        var requestType = classInfo.GenericTypes.RequestType;
        var resultType = classInfo.GenericTypes.ResultType;

        sb.AppendLine();
        sb.AppendLine($"        // {classInfo.ClassName} - {operationId}");

        if (classInfo.IsRequestHandler)
        {
            GenerateRequestEndpoint(sb, httpMethod, uriTemplate, requestType, resultType, attribute);
        }
        else if (classInfo.IsCommandHandler)
        {
            GenerateCommandEndpoint(sb, httpMethod, uriTemplate, requestType, attribute);
        }
    }

    static void GenerateRequestEndpoint(StringBuilder sb, string httpMethod, string uriTemplate, string requestType, string resultType, AttributeInfo attribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete ? "[global::Microsoft.AspNetCore.Http.AsParameters]" : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        sb.AppendLine($"        builder.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
        sb.AppendLine($"            \"{uriTemplate}\",");
        sb.AppendLine($"            async (");
        sb.AppendLine($"                [global::Microsoft.AspNetCore.Mvc.FromServices] global::Shiny.Mediator.IMediator mediator,");
        sb.AppendLine($"                {fromClause} {requestType} request,");
        sb.AppendLine($"                global::System.Threading.CancellationToken cancellationToken");
        sb.AppendLine($"            ) =>");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var result = await mediator");
        sb.AppendLine($"                    .Request(request, cancellationToken)");
        sb.AppendLine($"                    .ConfigureAwait(false);");
        sb.AppendLine($"                return global::Microsoft.AspNetCore.Http.Results.Ok(result);");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        )");

        ApplyEndpointConfiguration(sb, attribute);
        sb.AppendLine($"        ;");
    }

    static void GenerateCommandEndpoint(StringBuilder sb, string httpMethod, string uriTemplate, string requestType, AttributeInfo attribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete ? "[global::Microsoft.AspNetCore.Http.AsParameters]" : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        sb.AppendLine($"        builder.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
        sb.AppendLine($"            \"{uriTemplate}\",");
        sb.AppendLine($"            async (");
        sb.AppendLine($"                [global::Microsoft.AspNetCore.Mvc.FromServices] global::Shiny.Mediator.IMediator mediator,");
        sb.AppendLine($"                {fromClause} {requestType} command,");
        sb.AppendLine($"                global::System.Threading.CancellationToken cancellationToken");
        sb.AppendLine($"            ) =>");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                await mediator");
        sb.AppendLine($"                    .Send(command, cancellationToken)");
        sb.AppendLine($"                    .ConfigureAwait(false);");
        sb.AppendLine($"                return global::Microsoft.AspNetCore.Http.Results.Ok();");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        )");

        ApplyEndpointConfiguration(sb, attribute);
        sb.AppendLine($"        ;");
    }

    static void ApplyEndpointConfiguration(StringBuilder sb, AttributeInfo attribute)
    {
        // Apply WithName
        sb.AppendLine($"            .WithName(\"{attribute.OperationId}\")");

        // Apply authorization
        if (attribute.Properties.TryGetValue("RequiresAuthorization", out var requiresAuth) && (bool)requiresAuth)
        {
            if (attribute.Properties.TryGetValue("AuthorizationPolicies", out var policies) && policies is string[] policyArray && policyArray.Length > 0)
            {
                foreach (var policy in policyArray)
                {
                    sb.AppendLine($"            .RequireAuthorization(\"{policy}\")");
                }
            }
            else
            {
                sb.AppendLine($"            .RequireAuthorization()");
            }
        }

        if (attribute.Properties.TryGetValue("AllowAnonymous", out var allowAnon) && (bool)allowAnon)
        {
            sb.AppendLine($"            .AllowAnonymous()");
        }

        // Apply OpenAPI metadata
        if (attribute.Properties.TryGetValue("UseOpenApi", out var useOpenApi) && (bool)useOpenApi)
        {
            if (attribute.Properties.TryGetValue("DisplayName", out var displayName) && !string.IsNullOrEmpty((string)displayName))
            {
                sb.AppendLine($"            .WithDisplayName(\"{displayName}\")");
            }

            if (attribute.Properties.TryGetValue("Summary", out var summary) && !string.IsNullOrEmpty((string)summary))
            {
                sb.AppendLine($"            .WithSummary(\"{summary}\")");
            }

            if (attribute.Properties.TryGetValue("Description", out var description) && !string.IsNullOrEmpty((string)description))
            {
                sb.AppendLine($"            .WithDescription(\"{description}\")");
            }

            if (attribute.Properties.TryGetValue("Tags", out var tags) && tags is string[] tagArray && tagArray.Length > 0)
            {
                var tagList = string.Join("\", \"", tagArray);
                sb.AppendLine($"            .WithTags(\"{tagList}\")");
            }

            if (attribute.Properties.TryGetValue("GroupName", out var groupName) && !string.IsNullOrEmpty((string)groupName))
            {
                sb.AppendLine($"            .WithOpenApi(operation => {{ operation.Tags = new List<Microsoft.OpenApi.Models.OpenApiTag> {{ new() {{ Name = \"{groupName}\" }} }}; return operation; }})");
            }
        }

        if (attribute.Properties.TryGetValue("ExcludeFromDescription", out var excludeFromDesc) && (bool)excludeFromDesc)
        {
            sb.AppendLine($"            .ExcludeFromDescription()");
        }

        // Apply caching
        if (attribute.Properties.TryGetValue("CachePolicy", out var cachePolicy) && !string.IsNullOrEmpty((string)cachePolicy))
        {
            sb.AppendLine($"            .CacheOutput(\"{cachePolicy}\")");
        }

        // Apply CORS
        if (attribute.Properties.TryGetValue("CorsPolicy", out var corsPolicy) && !string.IsNullOrEmpty((string)corsPolicy))
        {
            sb.AppendLine($"            .RequireCors(\"{corsPolicy}\")");
        }

        // Apply rate limiting
        if (attribute.Properties.TryGetValue("RateLimitingPolicy", out var rateLimitPolicy) && !string.IsNullOrEmpty((string)rateLimitPolicy))
        {
            sb.AppendLine($"            .RequireRateLimiting(\"{rateLimitPolicy}\")");
        }
    }
}

// Supporting data classes
public class ClassInfo
{
    public string FullName { get; }
    public string ClassName { get; }
    public List<AttributeInfo> HttpAttributes { get; }
    public bool IsRequestHandler { get; }
    public bool IsCommandHandler { get; }
    public GenericTypeInfo GenericTypes { get; }

    public ClassInfo(
        string fullName,
        string className,
        List<AttributeInfo> httpAttributes,
        bool isRequestHandler,
        bool isCommandHandler,
        GenericTypeInfo genericTypes)
    {
        FullName = fullName;
        ClassName = className;
        HttpAttributes = httpAttributes;
        IsRequestHandler = isRequestHandler;
        IsCommandHandler = isCommandHandler;
        GenericTypes = genericTypes;
    }
}

public class AttributeInfo
{
    public string OperationId { get; }
    public string UriTemplate { get; }
    public string HttpMethod { get; }
    public Dictionary<string, object> Properties { get; }

    public AttributeInfo(
        string operationId,
        string uriTemplate,
        string httpMethod,
        Dictionary<string, object> properties)
    {
        OperationId = operationId;
        UriTemplate = uriTemplate;
        HttpMethod = httpMethod;
        Properties = properties;
    }
}

public class GenericTypeInfo
{
    public string RequestType { get; }
    public string ResultType { get; }

    public GenericTypeInfo(string requestType, string resultType)
    {
        RequestType = requestType;
        ResultType = resultType;
    }
}