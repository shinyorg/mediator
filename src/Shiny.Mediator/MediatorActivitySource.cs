using System.Diagnostics;

namespace Shiny.Mediator;


/// <summary>
/// Holds the <see cref="ActivitySource"/> used by the mediator to emit diagnostic activities.
/// Replace <see cref="Value"/> to redirect tracing to a custom source.
/// </summary>
public static class MediatorActivitySource
{
    /// <summary>
    /// The <see cref="ActivitySource"/> used by mediator infrastructure to start activities.
    /// </summary>
    public static ActivitySource Value { get; set; } = new("shiny.mediator");
}