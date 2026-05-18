using System.Collections.Concurrent;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Provides per-key async mutual exclusion. Concurrent callers using the same
/// key serialize behind a shared semaphore; different keys proceed in parallel.
/// Intended for use inside singleton components (middleware, cache services)
/// that need to serialize work-per-key without serializing across all keys.
/// </summary>
public sealed class KeyedLocker
{
    readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();

    /// <summary>
    /// Asynchronously acquires the lock for <paramref name="key"/>. Dispose the
    /// returned <see cref="IDisposable"/> (typically via <c>using</c>) to release.
    /// </summary>
    /// <param name="key">The key whose lock to acquire. Callers using the same key serialize behind it.</param>
    /// <param name="cancellationToken">Cancels the wait, not the held lock.</param>
    /// <returns>A disposable releaser. Must be disposed exactly once to release the lock.</returns>
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
