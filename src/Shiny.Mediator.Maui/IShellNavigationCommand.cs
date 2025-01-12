namespace Shiny.Mediator;

public interface IShellNavigationCommand : ICommand
{
    string PageUri { get; }
    string? ParameterName { get; }
    bool? Animate { get; set; }
}