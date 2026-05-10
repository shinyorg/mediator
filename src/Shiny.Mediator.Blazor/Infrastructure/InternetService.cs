using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class InternetService : IInternetService, IDisposable
{
    readonly IJSInProcessRuntime jsRuntime;
    
    public InternetService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = (JSInProcessRuntime)jsRuntime;
        
        this.dotNetRef = DotNetObjectReference.Create(this);
        this.jsRuntime.InvokeVoid("MediatorServices.subscribe", this.dotNetRef);
    }
    
    public event EventHandler<bool>? StateChanged;
    public bool IsAvailable => this.jsRuntime.Invoke<bool>("MediatorServices.isOnline");


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


    public void Dispose() => this.jsRuntime.InvokeVoid("MediatorServices.unsubscribe");
}