namespace Sample.Contracts;

public record MyPrismNavCommand(string Arg) : IPrismNavigationCommand
{
    public string? PrependedNavigationUri { get; }
    public string PageUri => "AnotherPage";
    public string? NavigationParameterName => null;
    public bool? IsAnimated { get; }
    public bool IsModal { get; }
    public INavigationService? Navigator { get; set; }
};