namespace Shiny.Mediator;

/// <summary>
/// Mediator command that, when dispatched, performs .NET MAUI Shell navigation via
/// <see cref="Shell.GoToAsync(ShellNavigationState, bool, System.Collections.Generic.IDictionary{string, object})"/>.
/// Handled by <see cref="Handlers.ShellNavigationCommandHandler{TCommand}"/>.
/// </summary>
public interface IShellNavigationCommand : ICommand
{
    /// <summary>
    /// Shell route or URI to navigate to (e.g. <c>"//main/details"</c>).
    /// </summary>
    string PageUri { get; }

    /// <summary>
    /// Optional key under which this command is passed in the Shell navigation parameters
    /// dictionary. Defaults to the command type name when not specified.
    /// </summary>
    string? ParameterName { get; }

    /// <summary>
    /// When set, controls whether the Shell transition is animated; <c>null</c> uses the
    /// platform default (<c>true</c>).
    /// </summary>
    bool? Animate { get; set; }
}