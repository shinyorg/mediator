using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shiny.Mediator.Server.Infrastructure;


public record ServerResult(
    Guid Id, 
    JsonObject Payload,
    string? ExceptionMessage
)
{
    public T As<T>() => Payload.Deserialize<T>()!;
    public bool IsSuccessful => ExceptionMessage == null;
    public void ThrowIfFailed()
    {
        if (ExceptionMessage != null)
            throw new InvalidOperationException(this.ExceptionMessage);
    }
}