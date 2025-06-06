using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Mocks;



public class MockInternetService : IInternetService
{
    public event EventHandler<bool>? StateChanged;
    TaskCompletionSource? completion;

    
    public bool IsAvailable
    {
        get;
        set
        {
            field = value;
            this.StateChanged?.Invoke(this, value);
            if (value)
                this.completion?.SetResult();
        }
    }
    
    
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;

        this.completion = new();
        await using (cancelToken.Register(() => this.completion.TrySetCanceled()));
        
        await this.completion.Task;
        this.completion = null;
    }
}