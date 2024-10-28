namespace Sample.Contracts;

public record MyPrismNavRequest(string Arg) : IPrismNavigationRequest
{
    public string? PrependedNavigationUri { get; }
    public string PageUri => "AnotherPage";
    public string? NavigationParameterName => null;
    public bool? IsAnimated { get; }
    public bool IsModal { get; }
    public INavigationService? Navigator { get; set; }
};