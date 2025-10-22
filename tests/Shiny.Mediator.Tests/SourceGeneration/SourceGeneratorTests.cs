using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class SourceGeneratorTests
{
    [Fact]
    public Task GenerateJsonConverter_ForPartialClass_ShouldGenerateCorrectCode()
    {
        // Arrange
        var source = """
            using System;
            
            [SourceGenerateJsonConverter]
            public partial class TestModel
            {
                public string Name { get; set; } = string.Empty;
                public int Age { get; set; }
                public string? Email { get; set; }
                public DateTime? BirthDate { get; set; }
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        return Verify(result);
    }

    [Fact]
    public Task GenerateJsonConverter_ForStruct_ShouldGenerateCorrectCode()
    {
        // Arrange
        var source = """
            using System;
            
            [SourceGenerateJsonConverter]
            public struct Point
            {
                public double X { get; set; }
                public double Y { get; set; }
                public string? Label { get; set; }
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        return Verify(result);
    }

    [Fact]
    public Task GenerateJsonConverter_ForRecord_ShouldGenerateCorrectCode()
    {
        // Arrange
        var source = """
            using System;
            
            [SourceGenerateJsonConverter]
            public partial record VehicleResult(int Id, string Manufacturer, string Model)
            {
                public string Name => $"{Manufacturer} {Model}";
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        return Verify(result);
    }

    [Fact]
    public void GenerateJsonConverter_ForNonPartialClass_ShouldReportDiagnostic()
    {
        // Arrange
        var source = """
            using System;
            
            [SourceGenerateJsonConverter]
            public class NonPartialClass
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        result.Diagnostics.ShouldNotBeEmpty();
        result.Diagnostics.ShouldContain(d => d.Id == "SJSG001");
        // result.Diagnostics.ShouldContain(d => d.GetMessage().Contains("must be declared as partial"));
    }

    [Fact]
    public void GenerateJsonConverter_WithNoProperties_ShouldGenerateEmptyConverter()
    {
        // Arrange
        var source = """
            [SourceGenerateJsonConverter]
            public partial class EmptyClass
            {
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedSources.ShouldNotBeEmpty();
    }

    [Fact(Skip = "TODO: Fix this test")]
    public void GenerateJsonConverter_WithPrivateProperties_ShouldIgnorePrivateProperties()
    {
        // Arrange
        var source = """
            [SourceGenerateJsonConverter]
            public partial class TestClass
            {
                public string PublicProp { get; set; } = string.Empty;
                private string PrivateProp { get; set; } = string.Empty;
                internal string InternalProp { get; set; } = string.Empty;
            }
            """;

        // Act
        var result = TestHelper.RunSourceGenerator(source);

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedSources.ShouldNotBeEmpty();
        
        var converterSource = result.GeneratedSources.FirstOrDefault(s => s.HintName.Contains("JsonConverter"));
        // converterSource.ShouldNotBeNull();
        // converterSource.SourceText.ToString().ShouldContain("PublicProp");
        // converterSource.SourceText.ToString().ShouldNotContain("PrivateProp");
        // converterSource.SourceText.ToString().ShouldNotContain("InternalProp");
    }
}

public static class TestHelper
{
    public static GeneratorRunResult RunSourceGenerator(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new JsonConverterSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);

        return driver.GetRunResult().Results[0];
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonConverterAttribute).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
