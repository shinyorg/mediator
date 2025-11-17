using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator;
using Shiny.Mediator.SourceGenerators;

var source = """
    using Shiny.Mediator;
    using Shiny.Mediator.Http;
    
    namespace TestNamespace;
    
    [Get("/api/users")]
    public  class GetUsersRequest : IRequest<UserListResult>
    {
    }
    
    public class UserListResult
    {
        public List<string> Users { get; set; } = new();
    }
    """;

var syntaxTree = CSharpSyntaxTree.ParseText(source);

var references = new List<MetadataReference>
{
    // Core types
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
    
    // HTTP types
    MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpRequestMessage).Assembly.Location),
    
    // Collection types
    MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
    
    // DI types
    MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
    
    // Shiny.Mediator types - THIS IS CRITICAL!
    MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
    
    // System.Runtime for netstandard compatibility
    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
};

var compilation = CSharpCompilation.Create(
    "TestAssembly",
    new[] { syntaxTree },
    references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

Console.WriteLine("Compilation diagnostics:");
foreach (var diag in compilation.GetDiagnostics())
{
    if (diag.Severity == DiagnosticSeverity.Error)
    {
        Console.WriteLine($"  ERROR: {diag}");
    }
}

var generator = new UserHttpClientSourceGenerator().AsSourceGenerator();

var optionsProvider = new TestAnalyzerConfigOptionsProvider("TestNamespace", null);

var driver = CSharpGeneratorDriver.Create(
    generators: new[] { generator },
    optionsProvider: optionsProvider
);

var runDriver = driver.RunGenerators(compilation);
var result = runDriver.GetRunResult().Results[0];

Console.WriteLine($"\nGenerator diagnostics: {result.Diagnostics.Length}");
foreach (var diag in result.Diagnostics)
{
    Console.WriteLine($"  {diag.Severity}: {diag}");
}

Console.WriteLine($"\nGenerated sources: {result.GeneratedSources.Length}");
foreach (var generatedSource in result.GeneratedSources)
{
    Console.WriteLine($"\n=== {generatedSource.HintName} ===");
    Console.WriteLine(generatedSource.SourceText.ToString());
}

// Test helper classes
class TestAnalyzerConfigOptionsProvider : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider
{
    private readonly TestAnalyzerConfigOptions options;

    public TestAnalyzerConfigOptionsProvider(string rootNamespace, string? httpNamespace)
    {
        options = new TestAnalyzerConfigOptions(rootNamespace, httpNamespace);
    }

    public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GlobalOptions => options;

    public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;

    public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
}

class TestAnalyzerConfigOptions : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> options;

    public TestAnalyzerConfigOptions(string rootNamespace, string? httpNamespace)
    {
        options = new Dictionary<string, string>
        {
            { "build_property.RootNamespace", rootNamespace },
            { "build_property.ShinyMediatorHttpNamespace", httpNamespace ?? rootNamespace }
        };
    }

    public override bool TryGetValue(string key, out string value)
    {
        return options.TryGetValue(key, out value!);
    }
}

