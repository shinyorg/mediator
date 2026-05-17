using System.Collections.Concurrent;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Provides per-key async mutual exclusion. Concurrent callers using the same
/// key serialize behind a shared semaphore; different keys proceed in parallel.
/// </summary>
public sealed class KeyedLocker
{
    readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();

    public async Task<IDisposable> LockAsync(string key, CancellationToken cancellationToken)
    {
        var sem = this.locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(sem);
    }


    sealed class Releaser(SemaphoreSlim sem) : IDisposable
    {
        int disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
                sem.Release();
        }
    }
}
