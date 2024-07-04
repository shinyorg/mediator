namespace Shiny.Mediator;

public interface IShellNavigationRequest : IRequest
{
    string PageUri { get; }
    string? ParameterName { get; }
    bool? Animate { get; set; }
}