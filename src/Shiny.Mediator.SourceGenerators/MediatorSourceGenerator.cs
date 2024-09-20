﻿using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGeneratorsKit;

namespace Shiny.Mediator.SourceGenerators;


[Generator]
public class MediatorSourceGenerator : ISourceGenerator
{
    readonly SyntaxReceiver syntaxReceiver = new RegisterHandlerAttributeSyntaxReceiver();
    
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(x => x.AddSource(
            "MediatorAttributes.g.cs", 
            SourceText.From(
                """
                // <auto-generated>
                // Code generated by Shiny Mediator Source Generator.
                // Changes may cause incorrect behavior and will be lost if the code is
                // regenerated.
                // </auto-generated>
                #nullable disable
                
                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
                internal sealed class SingletonHandlerAttribute : System.Attribute
                {
                }
                
                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
                internal sealed class ScopedHandlerAttribute : System.Attribute
                {
                }
                
                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
                internal sealed class SingletonMiddlewareAttribute : System.Attribute
                {
                }
                
                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
                internal sealed class ScopedMiddlewareAttribute : System.Attribute
                {
                }
                """,
                Encoding.UTF8
            )
        ));
        context.RegisterForSyntaxNotifications(() => syntaxReceiver);
    }

    
    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is SyntaxReceiver))
            return;

        // TODO: detect double registration of request handlers?
        // TODO: scopes
        // TODO: open middleware
        // TODO: this will be registered with multiple AddDiscoveredMediatorHandlers in the main app
        
        var classes = this.syntaxReceiver
            .Classes
            .GroupBy(x => x.ToDisplayString())
            .Select(x => x.First())
            .ToList();
        
        if (!classes.Any())
            return;
        
        var nameSpace = context.GetMSBuildProperty("RootNamespace") ?? context.Compilation.AssemblyName;
        var assName = context.Compilation.AssemblyName?.Replace(".", "_");
        var sb = new StringBuilder();
        sb
            .AppendLine("using Shiny.Mediator;")
            .AppendLine()
            .AppendLine($"namespace {nameSpace};")
            .AppendLine()
            .AppendLine("public static class __ShinyMediatorSourceGenExtensions {")
            .AppendLine($"\tpublic static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddDiscoveredMediatorHandlersFrom{assName}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)")
            .AppendLine("\t{");

        foreach (var clazz in classes)
        {
            var cls = clazz.ToDisplayString();
            if (clazz.HasAttribute("ScopedHandlerAttribute") || clazz.HasAttribute("ScopedMiddlewareAttribute"))
                sb.AppendLine($"\t\tservices.AddScopedAsImplementedInterfaces<{cls}>();");
            else
                sb.AppendLine($"\t\tservices.AddSingletonAsImplementedInterfaces<{cls}>();");
        }

        sb
            .AppendLine("\t\treturn services;")
            .AppendLine("\t}")
            .AppendLine("}");

        context.AddSource("__MediatorHandlersRegistration.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}