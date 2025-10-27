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
        var compilationAndClasses = context
            .CompilationProvider
            .Combine(classDeclarations.Collect());

        // Generate the output
        context.RegisterSourceOutput(
            compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc)
        );
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax;

    static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol is null)
            return null;

        // Check if this class implements IRequestHandler, ICommandHandler, or IStreamRequestHandler
        var isRequestHandler = false;
        var isCommandHandler = false;
        var isStreamRequestHandler = false;

        foreach (var intf in classSymbol.AllInterfaces)
        {
            if (intf.IsGenericType)
            {
                if (!isRequestHandler)
                    isRequestHandler = intf.OriginalDefinition.ToDisplayString() ==
                        "Shiny.Mediator.IRequestHandler<TRequest, TResult>";

                if (!isCommandHandler)
                    isCommandHandler = intf.OriginalDefinition.ToDisplayString() ==
                        "Shiny.Mediator.ICommandHandler<TCommand>";

                if (!isStreamRequestHandler)
                    isStreamRequestHandler = intf.OriginalDefinition.ToDisplayString() ==
                        "Shiny.Mediator.IStreamRequestHandler<TRequest, TResult>";
            }

            if (isRequestHandler && isCommandHandler && isStreamRequestHandler)
                break;
        }

        if (!isRequestHandler && !isCommandHandler && !isStreamRequestHandler)
            return null;

        // Find all Handle methods with MediatorHttp attributes
        var handleMethods = new List<IMethodSymbol>();

        foreach (var member in classSymbol.GetMembers("Handle"))
        {
            if (member is IMethodSymbol { Parameters.Length: 3 } method)
            {
                var p1Name = method.Parameters[1].ToDisplayString();
                var p2Name = method.Parameters[2].ToDisplayString();

                if (p1Name.StartsWith("Shiny.Mediator.IMediatorContext") &&
                    p2Name.StartsWith("System.Threading.CancellationToken"))
                {
                    handleMethods.Add(method);
                }
            }
        }

        if (!handleMethods.Any())
            return null;

        // Collect all MediatorHttp attributes from all Handle methods with their corresponding types
        var allHttpAttributes = new List<(AttributeData attribute, string parameterType, string resultType)>();
        foreach (var method in handleMethods)
        {
            var methodAttributes = method.GetAttributes()
                .Where(attr =>
                    attr.AttributeClass?.Name?.Contains("MediatorHttp") == true &&
                    !attr.AttributeClass.Name.Contains("Group"))
                .ToList();

            if (methodAttributes.Any())
            {
                // Get the first parameter type (the request/command type)
                var parameterType = method.Parameters[0].Type.ToDisplayString();

                // Determine result type based on method return type
                var resultType = "void";
                if (method.ReturnType is INamedTypeSymbol
                    {
                        IsGenericType: true, TypeArguments.Length: > 0
                    } namedReturnType)
                {
                    resultType = namedReturnType.TypeArguments[0].ToDisplayString();
                }

                foreach (var attr in methodAttributes)
                {
                    allHttpAttributes.Add((attr, parameterType, resultType));
                }
            }
        }

        if (!allHttpAttributes.Any())
        {
            // Check if this class has a group attribute - if so, keep it for merging
            var groupAttribute = classSymbol
                .GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "MediatorHttpGroupAttribute");

            if (groupAttribute == null)
                return null;

            // Return class info with group attribute but no HTTP attributes
            return new ClassInfo(
                classSymbol.ToDisplayString(),
                classSymbol.Name,
                new List<AttributeInfo>(),
                isRequestHandler,
                isCommandHandler,
                isStreamRequestHandler,
                GetGenericTypes(classSymbol),
                GetGroupAttributeInfo(groupAttribute)
            );
        }

        // Check for MediatorHttpGroupAttribute at class level
        var classGroupAttribute = classSymbol
            .GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "MediatorHttpGroupAttribute");

        return new ClassInfo(
            classSymbol.ToDisplayString(),
            classSymbol.Name,
            allHttpAttributes
                .Select(tuple => GetAttributeInfo(tuple.attribute, tuple.parameterType, tuple.resultType))
                .ToList(),
            isRequestHandler,
            isCommandHandler,
            isStreamRequestHandler,
            GetGenericTypes(classSymbol),
            classGroupAttribute != null ? GetGroupAttributeInfo(classGroupAttribute) : null
        );
    }

    static AttributeInfo GetAttributeInfo(AttributeData attribute, string parameterType, string resultType)
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
            if (namedArg.Value.Kind == TypedConstantKind.Array)
            {
                var arrayValues = namedArg.Value.Values.Select(v => v.Value?.ToString() ?? "").ToArray();
                properties[namedArg.Key] = arrayValues;
            }
            else
            {
                properties[namedArg.Key] = namedArg.Value.Value ?? "";
            }
        }

        return new AttributeInfo(operationId, uriTemplate, httpMethod, properties, parameterType, resultType);
    }

    static GroupAttributeInfo GetGroupAttributeInfo(AttributeData attribute)
    {
        var prefix = attribute.ConstructorArguments.Length > 0
            ? attribute.ConstructorArguments[0].Value?.ToString() ?? ""
            : "";

        var properties = new Dictionary<string, object>();
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Value.Kind == TypedConstantKind.Array)
            {
                var arrayValues = namedArg.Value.Values.Select(v => v.Value?.ToString() ?? "").ToArray();
                properties[namedArg.Key] = arrayValues;
            }
            else
            {
                properties[namedArg.Key] = namedArg.Value.Value ?? "";
            }
        }

        return new GroupAttributeInfo(prefix, properties);
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

        var streamRequestInterface = classSymbol.AllInterfaces.FirstOrDefault(i =>
            i.IsGenericType &&
            i.OriginalDefinition.ToDisplayString() == "Shiny.Mediator.IStreamRequestHandler<TRequest, TResult>");

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
        else if (streamRequestInterface != null)
        {
            requestType = streamRequestInterface.TypeArguments[0].ToDisplayString();
            resultType = streamRequestInterface.TypeArguments[1].ToDisplayString();
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

        // Group by full class name and merge partial classes
        var mergedClasses = validClasses
            .GroupBy(c => c.FullName)
            .Select(group => MergePartialClasses(group))
            .ToList();

        var nameSpace = compilation.AssemblyName ?? "Generated";


        // Generate endpoint mapping extensions
        GenerateEndpointMappingExtensions(context, nameSpace, mergedClasses);
    }

    static ClassInfo MergePartialClasses(IGrouping<string, ClassInfo> group)
    {
        var first = group.First();

        // If there's only one class, return it as-is
        if (group.Count() == 1)
            return first;

        // Merge all HTTP attributes from all partial classes and deduplicate by operation ID and HTTP method
        var allHttpAttributes = group
            .SelectMany(c => c.HttpAttributes)
            .GroupBy(attr => new { attr.OperationId, attr.HttpMethod, attr.UriTemplate })
            .Select(g => g.First()) // Take the first occurrence of each unique attribute
            .ToList();

        // Use the group attribute from the first class that has one
        var groupAttribute = group
            .Select(c => c.GroupAttribute)
            .FirstOrDefault(ga => ga != null);

        // Combine the boolean flags (OR operation for handler types)
        var isRequestHandler = group.Any(c => c.IsRequestHandler);
        var isCommandHandler = group.Any(c => c.IsCommandHandler);
        var isStreamRequestHandler = group.Any(c => c.IsStreamRequestHandler);

        // Use the generic types from the first class that has them
        var genericTypes = group
            .Select(c => c.GenericTypes)
            .FirstOrDefault(gt => !string.IsNullOrEmpty(gt.RequestType));

        return new ClassInfo(
            first.FullName,
            first.ClassName,
            allHttpAttributes,
            isRequestHandler,
            isCommandHandler,
            isStreamRequestHandler,
            genericTypes ?? first.GenericTypes,
            groupAttribute
        );
    }


    static void GenerateEndpointMappingExtensions(SourceProductionContext context, string nameSpace,
        List<ClassInfo> classes)
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
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine("public static class MediatorEndpoints");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Maps all generated mediator endpoints");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine(
            "    public static global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapGeneratedMediatorEndpoints(this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder)");
        sb.AppendLine("    {");

        // Group classes by their group attribute
        var groupedClasses = classes.GroupBy(c => c.GroupAttribute?.Prefix ?? "");

        foreach (var group in groupedClasses)
        {
            var groupPrefix = group.Key;
            var classesInGroup = group.ToList();

            if (!string.IsNullOrEmpty(groupPrefix))
            {
                // Create a group for classes with MediatorHttpGroupAttribute
                sb.AppendLine();
                sb.AppendLine($"        // Group: {groupPrefix}");
                sb.AppendLine($"        var group_{groupPrefix.Replace("/", "_").Replace("-", "_")} = builder.MapGroup(\"{groupPrefix}\");");

                // Get the first class's group attribute for group-level configuration
                var groupAttribute = classesInGroup.First().GroupAttribute;
                if (groupAttribute != null)
                {
                    ApplyGroupConfiguration(sb, $"group_{groupPrefix.Replace("/", "_").Replace("-", "_")}", groupAttribute);
                }

                foreach (var classInfo in classesInGroup)
                {
                    foreach (var attribute in classInfo.HttpAttributes)
                    {
                        GenerateGroupedEndpointMapping(sb, $"group_{groupPrefix.Replace("/", "_").Replace("-", "_")}", classInfo, attribute);
                    }
                }
            }
            else
            {
                // Handle classes without group attribute (direct on builder)
                foreach (var classInfo in classesInGroup)
                {
                    foreach (var attribute in classInfo.HttpAttributes)
                    {
                        GenerateEndpointMapping(sb, classInfo, attribute);
                    }
                }
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
        var requestType = attribute.ParameterType;
        var resultType = attribute.ResultType;

        // For non-grouped endpoints, keep the original URI template as-is
        sb.AppendLine();
        sb.AppendLine($"        // {classInfo.ClassName} - {operationId}");

        // Determine if this is a stream request, regular request or command
        if (classInfo.IsStreamRequestHandler)
        {
            GenerateStreamRequestEndpoint(sb, httpMethod, uriTemplate, requestType, resultType, attribute, classInfo.GroupAttribute);
        }
        else
        {
            var isRequest = resultType != "void" && resultType != "System.Threading.Tasks.Task";

            if (isRequest)
            {
                GenerateRequestEndpoint(sb, httpMethod, uriTemplate, requestType, resultType, attribute, classInfo.GroupAttribute);
            }
            else
            {
                GenerateCommandEndpoint(sb, httpMethod, uriTemplate, requestType, attribute, classInfo.GroupAttribute);
            }
        }
    }

    static void GenerateRequestEndpoint(StringBuilder sb, string httpMethod, string uriTemplate, string requestType,
        string resultType, AttributeInfo attribute, GroupAttributeInfo? groupAttribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete
            ? "[global::Microsoft.AspNetCore.Http.AsParameters]"
            : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        sb.AppendLine($"        builder.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
        sb.AppendLine($"            \"{uriTemplate}\",");
        sb.AppendLine($"            async (");
        sb.AppendLine(
            $"                [global::Microsoft.AspNetCore.Mvc.FromServices] global::Shiny.Mediator.IMediator mediator,");
        sb.AppendLine($"                {fromClause} {requestType} request,");
        sb.AppendLine($"                global::System.Threading.CancellationToken cancellationToken");
        sb.AppendLine($"            ) =>");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var result = await mediator");
        sb.AppendLine($"                    .Request(request, cancellationToken)");
        sb.AppendLine($"                    .ConfigureAwait(false);");
        sb.AppendLine($"                return global::Microsoft.AspNetCore.Http.TypedResults.Ok(result.Result);");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        )");

        ApplyEndpointConfiguration(sb, attribute, groupAttribute);
        sb.AppendLine($"        ;");
    }

    static void GenerateCommandEndpoint(StringBuilder sb, string httpMethod, string uriTemplate, string requestType,
        AttributeInfo attribute, GroupAttributeInfo? groupAttribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete
            ? "[global::Microsoft.AspNetCore.Http.AsParameters]"
            : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        sb.AppendLine($"        builder.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
        sb.AppendLine($"            \"{uriTemplate}\",");
        sb.AppendLine($"            async (");
        sb.AppendLine(
            $"                [global::Microsoft.AspNetCore.Mvc.FromServices] global::Shiny.Mediator.IMediator mediator,");
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

        ApplyEndpointConfiguration(sb, attribute, groupAttribute);
        sb.AppendLine($"        ;");
    }

    static void GenerateStreamRequestEndpoint(StringBuilder sb, string httpMethod, string uriTemplate, string requestType,
        string resultType, AttributeInfo attribute, GroupAttributeInfo? groupAttribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete
            ? "[global::Microsoft.AspNetCore.Http.AsParameters]"
            : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        sb.AppendLine($"        builder.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
        sb.AppendLine($"            \"{uriTemplate}\",");
        sb.AppendLine($"            (");
        sb.AppendLine(
            $"                [global::Microsoft.AspNetCore.Mvc.FromServices] global::Shiny.Mediator.IMediator mediator,");
        sb.AppendLine($"                {fromClause} {requestType} request,");
        sb.AppendLine($"                global::System.Threading.CancellationToken cancellationToken");
        sb.AppendLine($"            ) => StreamResultsAsync(mediator, request, cancellationToken)");
        sb.AppendLine($"        )");

        ApplyEndpointConfiguration(sb, attribute, groupAttribute);
        sb.AppendLine($"        ;");
        sb.AppendLine();
        sb.AppendLine($"        static async global::System.Collections.Generic.IAsyncEnumerable<{resultType}> StreamResultsAsync(");
        sb.AppendLine($"            global::Shiny.Mediator.IMediator mediator,");
        sb.AppendLine($"            {requestType} request,");
        sb.AppendLine($"            [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            var stream = mediator.Request(request, cancellationToken);");
        sb.AppendLine($"            await foreach (var item in stream.WithCancellation(cancellationToken))");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                yield return item.Result;");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        }}");
    }

    static void ApplyEndpointConfiguration(StringBuilder sb, AttributeInfo attribute, GroupAttributeInfo? groupAttribute)
    {
        // Apply WithName
        sb.AppendLine($"            .WithName(\"{attribute.OperationId}\")");

        // Apply authorization - check group first, then attribute
        var requiresAuth = false;
        string[]? authPolicies = null;
        var allowAnonymous = false;

        // Check group-level authorization settings first
        if (groupAttribute != null)
        {
            if (groupAttribute.Properties.TryGetValue("RequiresAuthorization", out var groupRequiresAuth))
                requiresAuth = (bool)groupRequiresAuth;

            if (groupAttribute.Properties.TryGetValue("AuthorizationPolicies", out var groupPolicies) &&
                groupPolicies is string[] groupPolicyArray)
                authPolicies = groupPolicyArray;

            if (groupAttribute.Properties.TryGetValue("AllowAnonymous", out var groupAllowAnon))
                allowAnonymous = (bool)groupAllowAnon;
        }

        // Override with attribute-level settings if present
        if (attribute.Properties.TryGetValue("RequiresAuthorization", out var attrRequiresAuth))
            requiresAuth = (bool)attrRequiresAuth;

        if (attribute.Properties.TryGetValue("AuthorizationPolicies", out var attrPolicies) &&
            attrPolicies is string[] attrPolicyArray)
            authPolicies = attrPolicyArray;

        if (attribute.Properties.TryGetValue("AllowAnonymous", out var attrAllowAnon))
            allowAnonymous = (bool)attrAllowAnon;

        // Apply authorization configuration
        if (allowAnonymous)
        {
            sb.AppendLine($"            .AllowAnonymous()");
        }
        else if (requiresAuth)
        {
            if (authPolicies != null && authPolicies.Length > 0)
            {
                foreach (var policy in authPolicies)
                {
                    sb.AppendLine($"            .RequireAuthorization(\"{policy}\")");
                }
            }
            else
            {
                sb.AppendLine($"            .RequireAuthorization()");
            }
        }

        // Display name - attribute takes precedence
        var displayName = "";
        if (groupAttribute?.Properties.TryGetValue("DisplayName", out var groupDisplayName) == true)
            displayName = (string)groupDisplayName;
        if (attribute.Properties.TryGetValue("DisplayName", out var attrDisplayName) &&
            !string.IsNullOrEmpty((string)attrDisplayName))
            displayName = (string)attrDisplayName;

        if (!string.IsNullOrEmpty(displayName))
            sb.AppendLine($"            .WithDisplayName(\"{displayName}\")");

        // Summary - attribute takes precedence
        var summary = "";
        if (groupAttribute?.Properties.TryGetValue("Summary", out var groupSummary) == true)
            summary = (string)groupSummary;
        if (attribute.Properties.TryGetValue("Summary", out var attrSummary) &&
            !string.IsNullOrEmpty((string)attrSummary))
            summary = (string)attrSummary;

        if (!string.IsNullOrEmpty(summary))
            sb.AppendLine($"            .WithSummary(\"{summary}\")");

        // Description - attribute takes precedence
        var description = "";
        if (groupAttribute?.Properties.TryGetValue("Description", out var groupDescription) == true)
            description = (string)groupDescription;
        if (attribute.Properties.TryGetValue("Description", out var attrDescription) &&
            !string.IsNullOrEmpty((string)attrDescription))
            description = (string)attrDescription;

        if (!string.IsNullOrEmpty(description))
            sb.AppendLine($"            .WithDescription(\"{description}\")");

        // Tags - merge group and attribute tags
        var allTags = new List<string>();
        if (groupAttribute?.Properties.TryGetValue("Tags", out var groupTags) == true &&
            groupTags is string[] groupTagArray)
            allTags.AddRange(groupTagArray);
        if (attribute.Properties.TryGetValue("Tags", out var attrTags) && attrTags is string[] attrTagArray)
            allTags.AddRange(attrTagArray);

        if (allTags.Any())
        {
            var tagList = string.Join("\", \"", allTags.Distinct());
            sb.AppendLine($"            .WithTags(\"{tagList}\")");
        }

        // Group name - attribute takes precedence, then group GroupName
        var groupName = "";
        if (groupAttribute?.Properties.TryGetValue("GroupName", out var groupGroupName) == true)
            groupName = (string)groupGroupName;
        if (attribute.Properties.TryGetValue("GroupName", out var attrGroupName) &&
            !string.IsNullOrEmpty((string)attrGroupName))
            groupName = (string)attrGroupName;

        if (!string.IsNullOrEmpty(groupName))
            sb.AppendLine($"            .WithOpenApi(operation => {{ operation.Tags = new List<Microsoft.OpenApi.Models.OpenApiTag> {{ new() {{ Name = \"{groupName}\" }} }}; return operation; }})");

        // Exclude from description - attribute takes precedence
        var excludeFromDesc = false;
        if (groupAttribute?.Properties.TryGetValue("ExcludeFromDescription", out var groupExcludeFromDesc) == true)
            excludeFromDesc = (bool)groupExcludeFromDesc;
        if (attribute.Properties.TryGetValue("ExcludeFromDescription", out var attrExcludeFromDesc))
            excludeFromDesc = (bool)attrExcludeFromDesc;

        if (excludeFromDesc)
            sb.AppendLine($"            .ExcludeFromDescription()");

        // Apply caching - attribute takes precedence
        var cachePolicy = "";
        if (groupAttribute?.Properties.TryGetValue("CachePolicy", out var groupCachePolicy) == true)
            cachePolicy = (string)groupCachePolicy;
        if (attribute.Properties.TryGetValue("CachePolicy", out var attrCachePolicy) &&
            !string.IsNullOrEmpty((string)attrCachePolicy))
            cachePolicy = (string)attrCachePolicy;

        if (!string.IsNullOrEmpty(cachePolicy))
            sb.AppendLine($"            .CacheOutput(\"{cachePolicy}\")");

        // Apply CORS - attribute takes precedence
        var corsPolicy = "";
        if (groupAttribute?.Properties.TryGetValue("CorsPolicy", out var groupCorsPolicy) == true)
            corsPolicy = (string)groupCorsPolicy;
        if (attribute.Properties.TryGetValue("CorsPolicy", out var attrCorsPolicy) &&
            !string.IsNullOrEmpty((string)attrCorsPolicy))
            corsPolicy = (string)attrCorsPolicy;

        if (!string.IsNullOrEmpty(corsPolicy))
            sb.AppendLine($"            .RequireCors(\"{corsPolicy}\")");

        // Apply rate limiting - attribute takes precedence
        var rateLimitPolicy = "";
        if (groupAttribute?.Properties.TryGetValue("RateLimitingPolicy", out var groupRateLimitPolicy) == true)
            rateLimitPolicy = (string)groupRateLimitPolicy;
        if (attribute.Properties.TryGetValue("RateLimitingPolicy", out var attrRateLimitPolicy) &&
            !string.IsNullOrEmpty((string)attrRateLimitPolicy))
            rateLimitPolicy = (string)attrRateLimitPolicy;

        if (!string.IsNullOrEmpty(rateLimitPolicy))
            sb.AppendLine($"            .RequireRateLimiting(\"{rateLimitPolicy}\")");
    }

    static void ApplyGroupConfiguration(StringBuilder sb, string groupVariableName, GroupAttributeInfo groupAttribute)
    {
        // Apply group-level authorization settings
        if (groupAttribute.Properties.TryGetValue("RequiresAuthorization", out var requiresAuth) && (bool)requiresAuth)
        {
            if (groupAttribute.Properties.TryGetValue("AuthorizationPolicies", out var policies) &&
                policies is string[] policyArray && policyArray.Length > 0)
            {
                foreach (var policy in policyArray)
                {
                    sb.AppendLine($"        {groupVariableName}.RequireAuthorization(\"{policy}\");");
                }
            }
            else
            {
                sb.AppendLine($"        {groupVariableName}.RequireAuthorization();");
            }
        }

        if (groupAttribute.Properties.TryGetValue("AllowAnonymous", out var allowAnonymous) && (bool)allowAnonymous)
        {
            sb.AppendLine($"        {groupVariableName}.AllowAnonymous();");
        }

        // Apply group-level CORS
        if (groupAttribute.Properties.TryGetValue("CorsPolicy", out var corsPolicy) &&
            !string.IsNullOrEmpty((string)corsPolicy))
        {
            sb.AppendLine($"        {groupVariableName}.RequireCors(\"{corsPolicy}\");");
        }

        // Apply group-level rate limiting
        if (groupAttribute.Properties.TryGetValue("RateLimitingPolicy", out var rateLimitPolicy) &&
            !string.IsNullOrEmpty((string)rateLimitPolicy))
        {
            sb.AppendLine($"        {groupVariableName}.RequireRateLimiting(\"{rateLimitPolicy}\");");
        }

        // Apply group-level caching
        if (groupAttribute.Properties.TryGetValue("CachePolicy", out var cachePolicy) &&
            !string.IsNullOrEmpty((string)cachePolicy))
        {
            sb.AppendLine($"        {groupVariableName}.CacheOutput(\"{cachePolicy}\");");
        }

        // Apply group-level tags
        if (groupAttribute.Properties.TryGetValue("Tags", out var tags) && tags is string[] tagArray && tagArray.Length > 0)
        {
            var tagList = string.Join("\", \"", tagArray);
            sb.AppendLine($"        {groupVariableName}.WithTags(\"{tagList}\");");
        }
        
        if (groupAttribute.Properties.TryGetValue("ExcludeFromDescription", out var description) && (bool)description)
        {
            sb.AppendLine($"        {groupVariableName}.ExcludeFromDescription();");
        }
    }

    static void GenerateGroupedEndpointMapping(StringBuilder sb, string groupVariableName, ClassInfo classInfo, AttributeInfo attribute)
    {
        var httpMethod = attribute.HttpMethod.ToLower();
        var operationId = attribute.OperationId;
        var uriTemplate = attribute.UriTemplate;
        var requestType = attribute.ParameterType;
        var resultType = attribute.ResultType;

        sb.AppendLine();
        sb.AppendLine($"        // {classInfo.ClassName} - {operationId}");

        // Determine if this is a stream request, regular request or command
        if (classInfo.IsStreamRequestHandler)
        {
            GenerateGroupedStreamRequestEndpoint(sb, groupVariableName, httpMethod, uriTemplate, requestType, resultType, attribute);
        }
        else
        {
            var isRequest = resultType != "void" && resultType != "System.Threading.Tasks.Task";

            if (isRequest)
            {
                GenerateGroupedRequestEndpoint(sb, groupVariableName, httpMethod, uriTemplate, requestType, resultType, attribute);
            }
            else
            {
                GenerateGroupedCommandEndpoint(sb, groupVariableName, httpMethod, uriTemplate, requestType, attribute);
            }
        }
    }

    static void GenerateGroupedRequestEndpoint(StringBuilder sb, string groupVariableName, string httpMethod, string uriTemplate, string requestType, string resultType, AttributeInfo attribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete
            ? "[global::Microsoft.AspNetCore.Http.AsParameters]"
            : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        sb.AppendLine($"        {groupVariableName}.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
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
        sb.AppendLine($"                return global::Microsoft.AspNetCore.Http.TypedResults.Ok(result.Result);");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        )");

        ApplyAttributeOnlyConfiguration(sb, attribute);
        sb.AppendLine($"        ;");
    }

    static void GenerateGroupedCommandEndpoint(StringBuilder sb, string groupVariableName, string httpMethod, string uriTemplate, string requestType, AttributeInfo attribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete
            ? "[global::Microsoft.AspNetCore.Http.AsParameters]"
            : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        sb.AppendLine(
            $"        {groupVariableName}.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
        sb.AppendLine($"            \"{uriTemplate}\",");
        sb.AppendLine($"            async (");
        sb.AppendLine(
            $"                [global::Microsoft.AspNetCore.Mvc.FromServices] global::Shiny.Mediator.IMediator mediator,");
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

        ApplyAttributeOnlyConfiguration(sb, attribute);
        sb.AppendLine($"        ;");
    }

    static void GenerateGroupedStreamRequestEndpoint(StringBuilder sb, string groupVariableName, string httpMethod, string uriTemplate, string requestType, string resultType, AttributeInfo attribute)
    {
        var isGetOrDelete = httpMethod == "get" || httpMethod == "delete";
        var fromClause = isGetOrDelete
            ? "[global::Microsoft.AspNetCore.Http.AsParameters]"
            : "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        // Generate a unique helper method name based on the operation ID
        var helperMethodName = $"StreamResultsAsync_{attribute.OperationId.Replace("-", "_").Replace(" ", "_")}";

        sb.AppendLine(
            $"        {groupVariableName}.Map{httpMethod.Substring(0, 1).ToUpper()}{httpMethod.Substring(1)}(");
        sb.AppendLine($"            \"{uriTemplate}\",");
        sb.AppendLine($"            (");
        sb.AppendLine(
            $"                [global::Microsoft.AspNetCore.Mvc.FromServices] global::Shiny.Mediator.IMediator mediator,");
        sb.AppendLine($"                {fromClause} {requestType} request,");
        sb.AppendLine($"                global::System.Threading.CancellationToken cancellationToken");
        sb.AppendLine($"            ) => {helperMethodName}(mediator, request, cancellationToken)");
        sb.AppendLine($"        )");

        ApplyAttributeOnlyConfiguration(sb, attribute);
        sb.AppendLine($"        ;");
        sb.AppendLine();
        sb.AppendLine($"        static async global::System.Collections.Generic.IAsyncEnumerable<{resultType}> {helperMethodName}(");
        sb.AppendLine($"            global::Shiny.Mediator.IMediator mediator,");
        sb.AppendLine($"            {requestType} request,");
        sb.AppendLine($"            [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            var stream = mediator.Request(request, cancellationToken);");
        sb.AppendLine($"            await foreach (var item in stream.WithCancellation(cancellationToken))");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                yield return item.Result;");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        }}");
    }

    static void ApplyAttributeOnlyConfiguration(StringBuilder sb, AttributeInfo attribute)
    {
        // Apply WithName
        sb.AppendLine($"            .WithName(\"{attribute.OperationId}\")");

        // Apply attribute-level authorization settings (group-level already applied to the group)
        if (attribute.Properties.TryGetValue("RequiresAuthorization", out var requiresAuth) && (bool)requiresAuth)
        {
            if (attribute.Properties.TryGetValue("AuthorizationPolicies", out var policies) &&
                policies is string[] policyArray && policyArray.Length > 0)
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

        if (attribute.Properties.TryGetValue("AllowAnonymous", out var allowAnonymous) && (bool)allowAnonymous)
        {
            sb.AppendLine($"            .AllowAnonymous()");
        }

        // Apply OpenAPI metadata
        if (attribute.Properties.TryGetValue("DisplayName", out var displayName) && !string.IsNullOrEmpty((string)displayName))
            sb.AppendLine($"            .WithDisplayName(\"{displayName}\")");

        if (attribute.Properties.TryGetValue("Summary", out var summary) && !string.IsNullOrEmpty((string)summary))
            sb.AppendLine($"            .WithSummary(\"{summary}\")");

        if (attribute.Properties.TryGetValue("Description", out var description) &&
            !string.IsNullOrEmpty((string)description))
            sb.AppendLine($"            .WithDescription(\"{description}\")");

        if (attribute.Properties.TryGetValue("Tags", out var tags) && tags is string[] tagArray &&
            tagArray.Length > 0)
        {
            var tagList = string.Join("\", \"", tagArray);
            sb.AppendLine($"            .WithTags(\"{tagList}\")");
        }

        if (attribute.Properties.TryGetValue("GroupName", out var groupName) &&
            !string.IsNullOrEmpty((string)groupName))
            sb.AppendLine($"            .WithOpenApi(operation => {{ operation.Tags = new List<Microsoft.OpenApi.Models.OpenApiTag> {{ new() {{ Name = \"{groupName}\" }} }}; return operation; }})");

        if (attribute.Properties.TryGetValue("ExcludeFromDescription", out var excludeFromDesc) && (bool)excludeFromDesc)
            sb.AppendLine($"            .ExcludeFromDescription()");

        if (attribute.Properties.TryGetValue("CachePolicy", out var cachePolicy) &&
            !string.IsNullOrEmpty((string)cachePolicy))
            sb.AppendLine($"            .CacheOutput(\"{cachePolicy}\")");

        if (attribute.Properties.TryGetValue("CorsPolicy", out var corsPolicy) &&
            !string.IsNullOrEmpty((string)corsPolicy))
            sb.AppendLine($"            .RequireCors(\"{corsPolicy}\")");

        if (attribute.Properties.TryGetValue("RateLimitingPolicy", out var rateLimitPolicy) &&
            !string.IsNullOrEmpty((string)rateLimitPolicy))
            sb.AppendLine($"            .RequireRateLimiting(\"{rateLimitPolicy}\")");
    }
}

// Supporting data classes
public record ClassInfo(
    string FullName,
    string ClassName,
    List < AttributeInfo > HttpAttributes,
    bool IsRequestHandler,
    bool IsCommandHandler,
    bool IsStreamRequestHandler,
    GenericTypeInfo GenericTypes,
    GroupAttributeInfo ? GroupAttribute = null
);

public record AttributeInfo(
    string OperationId,
    string UriTemplate,
    string HttpMethod,
    Dictionary<string, object> Properties,
    string ParameterType,
    string ResultType
);

public record GroupAttributeInfo(
    string Prefix,
    Dictionary<string, object> Properties
);

public record GenericTypeInfo(
    string RequestType,
    string ResultType
);