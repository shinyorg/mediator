using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class InternetService(IJSRuntime jsruntime) : IInternetService, IDisposable
{
    public bool IsAvailable => ((IJSInProcessRuntime)jsruntime).Invoke<bool>("MediatorServices.isOnline");


    [JSInvokable("MediatorServices.OnStatusChanged")]
    public void OnStatusChanged(bool isOnline)
    {
        if (isOnline)
            this.waitSource?.TrySetResult();
    }


    TaskCompletionSource? waitSource;
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;

        var objRef = DotNetObjectReference.Create(this);
        ((IJSInProcessRuntime)jsruntime).InvokeVoid("MediatorServices.subscribe", objRef);
        
        this.waitSource = new();
        await this.waitSource.Task.ConfigureAwait(false);
        this.waitSource = null;
    }

    public void Dispose() => ((IJSInProcessRuntime)jsruntime).InvokeVoid("MediatorServices.unsubscribe");
}