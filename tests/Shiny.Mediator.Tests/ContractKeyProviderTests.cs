using Shiny.Mediator.Infrastructure.Impl;

namespace Shiny.Mediator.Tests;

public class ContractKeyProviderTests
{
    [Theory]
    [InlineData("First1", null, "Shiny.Mediator.Tests.TestKeyCommand_FirstName_First1")]
    [InlineData("First2", "Last", "Shiny.Mediator.Tests.TestKeyCommand_FirstName_First2_LastName_Last")]
    public void ReflectKeyTests(string firstName, string? lastName, string expectedKey)
        => new DefaultContractKeyProvider(null)
            .GetContractKey(new TestKeyCommand(firstName, lastName))
            .ShouldBe(expectedKey);
}

public record TestKeyCommand(string FirstName, string? LastName) : ICommand;
