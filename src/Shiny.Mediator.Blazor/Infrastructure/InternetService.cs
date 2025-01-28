using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class InternetService(IJSRuntime jsruntime) : IInternetService, IDisposable
{
    public event EventHandler<bool> StateChanged;
    public bool IsAvailable => ((IJSInProcessRuntime)jsruntime).Invoke<bool>("MediatorServices.isOnline");


    [JSInvokable("MediatorServices.OnStatusChanged")]
    public void OnStatusChanged(bool isOnline)
    {
        this.StateChanged?.Invoke(this, isOnline);
        if (isOnline)
            this.waitSource?.TrySetResult();
    }


    DotNetObjectReference<InternetService>? dotNetRef;
    TaskCompletionSource? waitSource;
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;
        
        if (this.dotNetRef == null)
        {
            this.dotNetRef = DotNetObjectReference.Create(this);
            ((IJSInProcessRuntime)jsruntime).InvokeVoid("MediatorServices.subscribe", this.dotNetRef);
        }

        try
        {
            await using var _ = cancelToken.Register(() => this.waitSource?.TrySetCanceled());
            this.waitSource = new();
            await this.waitSource.Task.ConfigureAwait(false);
        }
        finally
        {
            this.waitSource = null;
        }
    }


    public void Dispose() => ((IJSInProcessRuntime)jsruntime).InvokeVoid("MediatorServices.unsubscribe");
}