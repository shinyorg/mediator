using Sample.Blazor.Contracts;
using Shiny.Mediator;

namespace Sample.Blazor.Handlers;

[SingletonHandler]
public class DoThingRequestHandler(IMediator mediator) : IRequestHandler<DoThing>
{
    public Task Handle(DoThing request, CancellationToken cancellationToken)
        => mediator.Publish(new TheThing(), cancellationToken);
}