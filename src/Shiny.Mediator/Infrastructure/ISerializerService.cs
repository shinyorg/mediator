namespace Shiny.Mediator.Infrastructure;

/// <summary>
/// Pluggable serializer used by mediator infrastructure (HTTP transport, persistence, etc.).
/// The default implementation uses <c>System.Text.Json</c>.
/// </summary>
public interface ISerializerService
{
    /// <summary>
    /// Serializes <paramref name="obj"/> to a string.
    /// </summary>
    string Serialize<T>(T obj);

    /// <summary>
    /// Deserializes <paramref name="content"/> into <typeparamref name="T"/>.
    /// </summary>
    T Deserialize<T>(string content);

    /// <summary>
    /// Deserializes <paramref name="content"/> into the runtime <paramref name="type"/>.
    /// </summary>
    object Deserialize(string content, Type type);

    /// <summary>
    /// Reads a stream of items of <typeparamref name="T"/> from a JSON (or similar) sequence. Used for stream
    /// responses returned by mediator HTTP handlers.
    /// </summary>
    IAsyncEnumerable<T> DeserlializeAsyncEnumerable<T>(Stream stream, CancellationToken cancellationToken = default);
}
