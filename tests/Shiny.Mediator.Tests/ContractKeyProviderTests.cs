using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Reflector;

namespace Shiny.Mediator.Tests;


public class ContractKeyProviderTests
{
    [Theory]
    [InlineData("First1", null, "Shiny.Mediator.Tests.TestKeyCommand_FirstName_First1")]
    [InlineData("First2", "Last", "Shiny.Mediator.Tests.TestKeyCommand_FirstName_First2_LastName_Last")]
    public void DefaultProvider(string firstName, string? lastName, string expectedKey)
        => new DefaultContractKeyProvider(null)
            .GetContractKey(new TestKeyCommand(firstName, lastName))
            .ShouldBe(expectedKey);


    [Theory]
    [InlineData("First1", null, "Shiny.Mediator.Tests.TestKeyCommand_FirstName_First1")]
    [InlineData("First2", "Last", "Shiny.Mediator.Tests.TestKeyCommand_FirstName_First2_LastName_Last")]
    public void SmartProvider_NonReflectorType(string firstName, string? lastName, string expectedKey)
        => new SmartContractKeyProvider(null, new ConfigurationManager())
            .GetContractKey(new TestKeyCommand(firstName, lastName))
            .ShouldBe(expectedKey);
    
    
    [Theory]
    [InlineData("First1", null, "Shiny.Mediator.Tests.ReflectorKeyCommand_FirstName_First1")]
    [InlineData("First2", "Last", "Shiny.Mediator.Tests.ReflectorKeyCommand_FirstName_First2_LastName_Last")]
    public void SmartProvider_NoConfig(string firstName, string? lastName, string expectedKey)
        => new SmartContractKeyProvider(null, new ConfigurationManager())
            .GetContractKey(new ReflectorKeyCommand(firstName, lastName))
            .ShouldBe(expectedKey);


    [Theory]
    [InlineData("Allan", null, "TEST_{FirstName}_{LastName}", "TEST_Allan_")]
    [InlineData(null, "Ritchie", "TEST_{FirstName}_{LastName}", "TEST__Ritchie")]
    [InlineData("Date", "Test", "T_{CreatedAt:yyyy-MM-dd}", "T_2025-07-22")]
    public void SmartProvider_FromConfig(string firstName, string? lastName, string configParseKey, string expectedKey)
    {
        var contract = new ReflectorKeyCommand(firstName, lastName) { CreatedAt = new(2025, 7, 22) };
        var type = contract.GetType();
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string>
        {
            { $"Mediator:Keys:{type.Namespace}.{type.Name}", configParseKey }
        }!);
        
        var provider = new SmartContractKeyProvider(null, config);
            
        provider.GetContractKey(contract).ShouldBe(expectedKey, "First pass failed to parse");
        provider.GetContractKey(contract).ShouldBe(expectedKey, "Second pass failed to parse");
    }
}

public record TestKeyCommand(string FirstName, string? LastName) : ICommand
{
    public DateTime? CreatedAt { get; set; }
} 

[Reflector]
public partial record ReflectorKeyCommand(string FirstName, string? LastName) : ICommand
{
    public DateTime? CreatedAt { get; set; }
} 
