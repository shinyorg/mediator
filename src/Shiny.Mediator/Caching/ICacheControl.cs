namespace Shiny.Mediator.Caching;


public interface ICacheControl
{
    bool ForceRefresh { get; set; }
    TimeSpan? AbsoluteExpiration { get; set; }
    TimeSpan? SlidingExpiration { get; set; }
}