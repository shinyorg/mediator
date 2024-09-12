using Microsoft.Extensions.Caching.Memory;

namespace Shiny.Mediator;


public interface ICacheControl
{
    bool ForceRefresh { get; set; }
    Action<ICacheEntry>? SetEntry { get; set; }
}