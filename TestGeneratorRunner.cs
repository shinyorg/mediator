using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;
using Shiny.Mediator;

namespace TestRunner;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing UserHttpClientSourceGenerator...");
        
        var source = """
            using Shiny.Mediator;
            using Shiny.Mediator.Http;
            
            namespace TestNamespace;
            
            [Get("/api/users")]
            public class GetUsersRequest : IRequest<UserListResult>
            {
            }
            
            public class UserListResult
            {
                public List<string> Users { get; set; } = new();
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Check for compilation errors
        var diagnostics = compilation.GetDiagnostics();
        foreach (var diag in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            Console.WriteLine($"Compilation Error: {diag}");
        }

        var generator = new UserHttpClientSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var result = driver.GetRunResult();
        
        Console.WriteLine($"Generated {result.GeneratedTrees.Length} files");
        
        foreach (var generatedTree in result.GeneratedTrees)
        {
            Console.WriteLine($"\n=== {generatedTree.FilePath} ===");
            Console.WriteLine(generatedTree.GetText());
        }
        
        if (result.Diagnostics.Any())
        {
            Console.WriteLine("\nGenerator Diagnostics:");
            foreach (var diag in result.Diagnostics)
            {
                Console.WriteLine($"{diag.Severity}: {diag.GetMessage()}");
            }
        }
    }
}

