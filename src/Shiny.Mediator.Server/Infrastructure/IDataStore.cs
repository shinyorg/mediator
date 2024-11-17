using Shiny.Mediator.Server.Models;

namespace Shiny.Mediator.Server.Infrastructure;


public interface IDataStore
{
    Task Queue(Message message);

    Task FailExpiredMessages();
    Task<IEnumerable<Message>> GetInboxMessages(string cluster);
    // Task<IEnumerable<Message>> GetInboxMessagesDue(string cluster, TimeSpan forward);
    
    Task MarkProcessed(Guid messageId);
    Task ProcessError(Guid messageId, Exception exception);
}