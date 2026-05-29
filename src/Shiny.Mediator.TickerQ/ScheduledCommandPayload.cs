namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Serialized form of a scheduled command persisted in a TickerQ time ticker request. Holds the command's
/// assembly-qualified type name and its serialized body so <see cref="ScheduledCommandRunner"/> can rehydrate
/// and re-dispatch it when the ticker fires.
/// </summary>
public class ScheduledCommandPayload
{
    /// <summary>
    /// Assembly-qualified name of the command type, used to resolve the runtime <see cref="Type"/> on execution.
    /// </summary>
    public string CommandType { get; set; } = null!;

    /// <summary>
    /// The command instance serialized via the mediator's <see cref="ISerializerService"/>.
    /// </summary>
    public string CommandJson { get; set; } = null!;
}
