using System.Diagnostics.CodeAnalysis;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;

namespace Shiny.Mediator.Tests;


public class ContractKeyProviderTests
{
    [Fact]
    public void ContractKey_WinsOverKeySource()
    {
        var provider = new DefaultContractKeyProvider(null, [new FixedKeySource("source")]);
        provider.GetContractKey(new SelfKeyedContract()).ShouldBe("self");
    }


    [Fact]
    public void KeySource_WinsOverReflection()
    {
        var provider = new DefaultContractKeyProvider(null, [new FixedKeySource("source")]);
        provider.GetContractKey(new PlainContract("a")).ShouldBe("source");
    }


    [Fact]
    public void FallsBackToReflection_WhenNoSourceMatches()
    {
        var provider = new DefaultContractKeyProvider(null, [new NoMatchKeySource()]);
        provider.GetContractKey(new PlainContract("a")).ShouldBe(typeof(PlainContract).FullName + "_Value_a");
    }


    [Fact]
    public void KeySources_ResolvedFromContainer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IContractKeySource>(new FixedKeySource("from-di"));
        services.AddShinyMediator(includeStandardMiddleware: false);

        var key = services
            .BuildServiceProvider()
            .GetRequiredService<IContractKeyProvider>()
            .GetContractKey(new PlainContract("a"));

        key.ShouldBe("from-di");
    }


    [Fact(DisplayName = "SetContractKeyProvider replaces the default even when called after AddShinyMediator (#61)")]
    public void SetContractKeyProvider_AfterAddShinyMediator_Replaces()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(includeStandardMiddleware: false);
        new ShinyMediatorBuilder(services).SetContractKeyProvider<CustomKeyProvider>();

        services.Count(x => x.ServiceType == typeof(IContractKeyProvider)).ShouldBe(1);
        services
            .BuildServiceProvider()
            .GetRequiredService<IContractKeyProvider>()
            .ShouldBeOfType<CustomKeyProvider>();
    }


    [Fact]
    public void SetContractKeyProvider_InConfigurator_Replaces()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(x => x.SetContractKeyProvider<CustomKeyProvider>(), false);

        services.Count(x => x.ServiceType == typeof(IContractKeyProvider)).ShouldBe(1);
        services
            .BuildServiceProvider()
            .GetRequiredService<IContractKeyProvider>()
            .ShouldBeOfType<CustomKeyProvider>();
    }
}


file record PlainContract(string Value) : IRequest<string>;

file record SelfKeyedContract : IRequest<string>, IContractKey
{
    public string GetKey() => "self";
}

file class FixedKeySource(string value) : IContractKeySource
{
    public bool TryGetKey(object contract, [NotNullWhen(true)] out string? key)
    {
        key = value;
        return true;
    }
}

file class NoMatchKeySource : IContractKeySource
{
    public bool TryGetKey(object contract, [NotNullWhen(true)] out string? key)
    {
        key = null;
        return false;
    }
}

file class CustomKeyProvider : IContractKeyProvider
{
    public string GetContractKey(object contract) => "custom";
}
