namespace Shiny.Mediator.Server.Models;

public class MessageError
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    
    public string Message { get; set; }
    public string StackTrace { get; set; }
    
    public int Attempts { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}