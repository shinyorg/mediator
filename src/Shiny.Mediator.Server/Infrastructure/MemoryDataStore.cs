using Shiny.Mediator.Server.Models;

namespace Shiny.Mediator.Server.Infrastructure;

public class MemoryDataStore : IDataStore
{
    readonly List<Message> inbox = new();
    readonly List<Message> processed = new();
    readonly List<Message> deadletter = new();
    readonly List<MessageError> errors = new();
    
    
    public Task Queue(Message message, CancellationToken cancellationToken)
    {
        lock (this.inbox)
        {
            this.inbox.Add(message);
        }

        return Task.CompletedTask;
    }

    public Task FailExpiredMessages(CancellationToken cancellationToken)
    {
        lock (this.inbox)
        {
            var removed = this.inbox
                .Where(x => x.Expires > DateTimeOffset.UtcNow)
                .ToList();

            foreach (var message in removed)
            {
                this.inbox.Remove(message);
                this.deadletter.Add(message);
            }
        }
        return Task.CompletedTask;
    }
    

    public Task<IEnumerable<Message>> GetInboxMessages(string cluster, CancellationToken cancellationToken)
    {
        lock (this.inbox)
        {
            var list = this.inbox
                .Where(x => 
                    // TODO: filter on cluster that owns the command
                    x.DateScheduled >= DateTimeOffset.UtcNow
                )
                .ToList();
            
            return Task.FromResult<IEnumerable<Message>>(list);
        }
    }

    public Task MarkProcessed(Guid messageId, CancellationToken cancellationToken)
    {
        lock (this.inbox)
        {
            var msg = this.inbox.FirstOrDefault(x => x.MessageId == messageId);
            if (msg != null)
            {
                msg.Processed = DateTimeOffset.UtcNow;
                this.inbox.Remove(msg);
                this.processed.Add(msg);
            }
        }
        return Task.CompletedTask;
    }
    

    public Task ProcessError(Guid messageId, Exception exception, CancellationToken cancellationToken)
    {
        lock (this.inbox)
        {
            var msg = this.inbox.FirstOrDefault(x => x.MessageId == messageId);

            if (msg != null)
            {
                var errorCount = this.errors.Count(x => x.MessageId == messageId) + 1;
                if (errorCount > 3)
                {
                    this.inbox.Remove(msg);
                    this.deadletter.Add(msg);
                }
                this.errors.Add(new MessageError
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace!,
                    Attempts = errorCount,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }
        return Task.CompletedTask;
    }
}