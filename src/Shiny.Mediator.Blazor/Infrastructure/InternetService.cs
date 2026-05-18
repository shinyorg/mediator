using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


/// <summary>
/// Blazor <see cref="IInternetService"/> backed by JS interop. Subscribes to browser
/// <c>online</c>/<c>offline</c> events through the <c>MediatorServices</c> JS module and exposes
/// the current <c>navigator.onLine</c> state.
/// </summary>
public class InternetService : IInternetService, IDisposable
{
    readonly IJSInProcessRuntime jsRuntime;

    /// <summary>
    /// Creates the service and subscribes to JS connectivity notifications via the
    /// <c>MediatorServices.subscribe</c> JS function.
    /// </summary>
    public InternetService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = (JSInProcessRuntime)jsRuntime;

        this.dotNetRef = DotNetObjectReference.Create(this);
        this.jsRuntime.InvokeVoid("MediatorServices.subscribe", this.dotNetRef);
    }

    /// <inheritdoc/>
    public event EventHandler<bool>? StateChanged;

    /// <inheritdoc/>
    public bool IsAvailable => this.jsRuntime.Invoke<bool>("MediatorServices.isOnline");


    /// <summary>
    /// JS-invokable callback fired by the <c>MediatorServices</c> JS module whenever the browser's
    /// online state changes. Raises <see cref="StateChanged"/> and releases pending
    /// <see cref="WaitForAvailable"/> waiters when the browser comes online.
    /// </summary>
    [JSInvokable("MediatorServices.OnStatusChanged")]
    public void OnStatusChanged(bool isOnline)
    {
        this.StateChanged?.Invoke(this, isOnline);
        if (isOnline)
        {
            List<TaskCompletionSource> snapshot;
            lock (this.waiters)
            {
                snapshot = this.waiters.ToList();
                this.waiters.Clear();
            }
            foreach (var tcs in snapshot)
                tcs.TrySetResult();
        }
    }


    DotNetObjectReference<InternetService>? dotNetRef;
    readonly List<TaskCompletionSource> waiters = new();

    /// <inheritdoc/>
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;

        var tcs = new TaskCompletionSource();
        lock (this.waiters)
            this.waiters.Add(tcs);

        await using var _ = cancelToken.Register(() =>
        {
            tcs.TrySetCanceled();
            lock (this.waiters)
                this.waiters.Remove(tcs);
        });
        await tcs.Task.ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public void Dispose() => this.jsRuntime.InvokeVoid("MediatorServices.unsubscribe");
}