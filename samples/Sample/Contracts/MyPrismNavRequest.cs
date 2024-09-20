namespace Sample.Contracts;

public record MyPrismNavRequest(string Arg) : IPrismNavigationRequest
{
    public string PageUri => "AnotherPage";
    public string? NavigationParameterName => null;
    public INavigationService? Navigator { get; set; }
};