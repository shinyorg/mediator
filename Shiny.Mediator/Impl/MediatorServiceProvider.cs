using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;

public class MediatorServiceProvider : 
    IServiceProvider, 
    ISupportRequiredService, 
    IKeyedServiceProvider, 
    IServiceProviderIsKeyedService, 
    IDisposable, 
    IAsyncDisposable
{
    bool disposed;
    
    public MediatorServiceProvider()
    {
    }

    
    // for every IEvent<> that comes out, we want to store it the main/singleton mediator and remove it when the childscope is disposed
    public object? GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }

    public object GetRequiredService(Type serviceType)
    {
        throw new NotImplementedException();
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        throw new NotImplementedException();
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        throw new NotImplementedException();
    }
    
    

    public bool IsService(Type serviceType)
    {
        throw new NotImplementedException();
    }

    public bool IsKeyedService(Type serviceType, object? serviceKey)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources;
    /// <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            this.disposed = true;
            if (disposing)
            {
                // this.lifetimeScope.Dispose();
            }
        }
    }
   
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
   
    /// <summary>
    /// Performs a dispose operation asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!this.disposed)
        {
            this.disposed = true;
            // await _lifetimeScope.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }    
}