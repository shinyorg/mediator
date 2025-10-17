using Sample.Blazor.Contracts;
using Shiny.Mediator;

namespace Sample.Blazor.Handlers;


[MediatorSingleton]
public partial class DoThingRequestHandler(IMediator mediator) : IRequestHandler<DoThing, int>
{
    [OfflineAvailable]
    public async Task<int> Handle(DoThing request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var num = new Random().Next(1, 1000000);
        var value = $"{request.Text} - number: {num}";
        await mediator.Publish(new TheThing(value), cancellationToken);
            
        return num;
    }
}