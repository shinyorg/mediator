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
            this.waitSource?.TrySetResult();
    }


    DotNetObjectReference<InternetService>? dotNetRef;
    TaskCompletionSource? waitSource;
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;

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


    public void Dispose() => this.jsRuntime.InvokeVoid("MediatorServices.unsubscribe");
}