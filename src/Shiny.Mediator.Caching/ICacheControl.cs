using Microsoft.Extensions.Caching.Memory;

namespace Shiny.Mediator;


public interface ICacheControl
{
    bool ForceRefresh { get; }
    void Set(ICacheEntry entry);
}