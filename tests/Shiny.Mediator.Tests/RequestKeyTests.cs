namespace Shiny.Mediator.Tests;

public class RequestKeyTests
{
    [Theory]
    [InlineData("First1", null, "Shiny.Mediator.Tests.TestKeyRequest_FirstName_First1")]
    [InlineData("First2", "Last", "Shiny.Mediator.Tests.TestKeyRequest_FirstName_First2_LastName_Last")]
    public void ReflectKeyTests(string firstName, string? lastName, string expectedKey)
        => new TestKeyCommand(firstName, lastName).GetKey().Should().Be(expectedKey);
}

public record TestKeyCommand(string FirstName, string? LastName) : ICommand, IRequestKey
{
    public string GetKey() => this.ReflectKey();
}