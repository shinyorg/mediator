namespace Shiny.Mediator.Infrastructure.Impl;


public partial class Mediator(
    IServiceProvider services,
    IEnumerable<IEventCollector> collectors
) : IMediator
{
}