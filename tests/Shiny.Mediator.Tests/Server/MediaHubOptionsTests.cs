using Shiny.Mediator.Server.Client;

namespace Shiny.Mediator.Tests.Server;


public class MediaHubOptionsTests
{
    [Fact]
    public void TypeFoundInAssembly()
    {
        var options = new MediatorHubOptions();
        options.Map(typeof(CollectorTestEvent).Assembly, new Uri("http://assembly"));

        options.GetUriForContract(typeof(CollectorTestEvent)).Should().Be("http://assembly");
        options.GetUriForContract(typeof(CollectorTestRequest)).Should().Be("http://assembly");
    }


    [Fact]
    public void SpecificTypeFound()
    {
        var options = new MediatorHubOptions();
        options.Map<CollectorTestEvent>(new Uri("http://type-specific"));
        
        options.GetUriForContract(typeof(CollectorTestEvent)).Should().Be("http://type-specific");
        options.GetUriForContract(typeof(CollectorTestRequest)).Should().BeNull();
    }

    
    [Theory]
    [InlineData(typeof(CollectorTestRequest), "Shiny.Mediator.*", true)]
    [InlineData(typeof(CollectorTestRequest), "Shiny.Mediator", false)]
    [InlineData(typeof(CollectorTestRequest), "Shiny.Mediator.Tests.*", true)]
    [InlineData(typeof(CollectorTestRequest), "*", true)]
    public void NamespaceSearchFound(Type contractType, string namespaceValue, bool expectedFind)
    {
        var options = new MediatorHubOptions();
        options.Map(namespaceValue, new Uri("http://namespace"));

        var type = options.GetUriForContract(contractType);
        if (expectedFind)
            type.Should().Be(new Uri("http://namespace"));
        else
            type.Should().BeNull();
    }
}