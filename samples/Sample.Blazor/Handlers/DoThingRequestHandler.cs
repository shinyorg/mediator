using Sample.Blazor.Contracts;
using Shiny.Mediator;

namespace Sample.Blazor.Handlers;

[SingletonHandler]
public class DoThingRequestHandler(IMediator mediator) : IRequestHandler<DoThing, int>
{
    [OfflineAvailable]
    public async Task<int> Handle(DoThing request, RequestContext<DoThing> context, CancellationToken cancellationToken)
    {
        var num = new Random().Next(1, 1000000);
        var value = $"{request.Text} - number: {num}";
        await mediator.Publish(new TheThing(value), cancellationToken);
            
        return num;
    }
}