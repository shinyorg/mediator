using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.Contracts.SourceGenerators;

namespace Shiny.Mediator.Tests;


public class ContractKeySourceGeneratorTests
{
    [Fact]
    public Task Driver_Success()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""MyContract_{MyProperty}_{MyDate:MMM-dd}"")]
public partial class MyClass
{
    public string MyProperty { get; set; }
    public string AnotherProperty { get; set; }
    public DateTime? MyDate { get; set; }
}");
        return Verify(driver.GetRunResult().Results.FirstOrDefault());
    }

    [Fact]
    public Task RunResult_Success()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""MyContract_{MyProperty}_{MyDate:MMM-dd}"")]
public partial class MyClass
{
    public string MyProperty { get; set; }
    public string AnotherProperty { get; set; }
    public DateTime? MyDate { get; set; }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    [Fact]
    public Task RunResult_WithDateTimeFormatting()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""Event_{EventId}_{EventDate:yyyy-MM-dd}_{Status}"")]
public partial class EventContract
{
    public int EventId { get; set; }
    public DateTime EventDate { get; set; }
    public string Status { get; set; }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    [Fact]
    public Task RunResult_WithRecord()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""Record_{Id}_{Name}"")]
public partial record MyRecord(int Id, string Name);");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    [Fact]
    public Task RunResult_NonPartialClass_ReportsError()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""NonPartial_{Id}"")]
public class NonPartialClass
{
    public int Id { get; set; }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(0);
        result.Diagnostics.Length.ShouldBe(1);
        result.Diagnostics[0].Id.ShouldBe("SMC001");
        return Verify(result);
    }

    [Fact]
    public Task RunResult_MissingProperty_ReportsError()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""Missing_{Id}_{NonExistentProperty}"")]
public partial class MissingPropertyClass
{
    public int Id { get; set; }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(0);
        result.Diagnostics.Length.ShouldBe(1);
        result.Diagnostics[0].Id.ShouldBe("SMC002");
        return Verify(result);
    }

    [Fact]
    public Task RunResult_MultiplePropertiesWithNamespace()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

namespace MyNamespace.SubNamespace;

[ContractKey(""Complex_{UserId:000}_{Action}_{Timestamp:yyyy-MM-dd-HH-mm-ss}"")]
public partial class ComplexContract
{
    public int UserId { get; set; }
    public string Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string UnusedProperty { get; set; }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    [Fact]
    public Task RunResult_WithRecord_PrimaryConstructor()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""Product_{Id}_{Name}_{Price:C}"")]
public partial record Product(int Id, string Name, decimal Price);");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    [Fact]
    public Task RunResult_WithRecord_MixedProperties()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""Order_{OrderId}_{CustomerId}_{OrderDate:yyyy-MM-dd}_{Status}"")]
public partial record Order(int OrderId, int CustomerId)
{
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = ""Pending"";
    public decimal Total { get; set; }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    [Fact]
    public Task RunResult_WithRecord_NullableTypes()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""Event_{EventId}_{EventDate:MMM-dd}_{Description}"")]
public partial record Event(int EventId, DateTime? EventDate, string? Description);");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    [Fact]
    public Task RunResult_WithNonNullableValueTypes()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;

[ContractKey(""Test_{Name}_{DoubleValue}_{IntValue}_{NullableIntValue}_{Timestamp:yyyyMMddHHmmss}"")]
public partial class TestContractKey
{
    public string Name { get; set; }
    public DateTime? Timestamp { get; set; }
    public double DoubleValue { get; set; }
    public int IntValue { get; set; }
    public int? NullableIntValue { get; set; }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        result.GeneratedSources.Length.ShouldBe(1);
        return Verify(result);
    }

    static GeneratorDriver BuildDriver(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        
        // Add minimal references for compilation
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequestKey).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        };

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], references, options);

        var generator = new ContractKeySourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
    }
}