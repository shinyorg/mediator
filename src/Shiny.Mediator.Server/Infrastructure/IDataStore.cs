using Shiny.Mediator.Server.Models;

namespace Shiny.Mediator.Server.Infrastructure;


public interface IDataStore
{
    // Task RegisterCluster(ClusterRegistration registration);
    
    Task Queue(Message message, CancellationToken cancellationToken);

    Task FailExpiredMessages(CancellationToken cancellationToken);
    Task<IEnumerable<Message>> GetInboxMessages(string cluster, CancellationToken cancellationToken);
    // Task<IEnumerable<Message>> GetInboxMessagesDue(string cluster, TimeSpan forward);
    
    Task MarkProcessed(Guid messageId, CancellationToken cancellationToken);
    Task ProcessError(Guid messageId, Exception exception, CancellationToken cancellationToken);
}