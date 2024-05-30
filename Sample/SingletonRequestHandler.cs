namespace Sample;


public class SingletonRequestHandler(IMediator mediator, AppSqliteConnection data) : IRequestHandler<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Handle(MyMessageRequest request, CancellationToken cancellationToken)
    {
        // TODO: I would normally want to fire this AFTER the return though
            // this is likely why service bus frameworks have a return method
            // could have a pre/post on handlers
        var e = new MyMessageEvent(
            request.Arg,
            request.FireAndForgetEvents,
            request.ParallelEvents
        );
        await data.Log("SingletonRequestHandler", e);
        await mediator.Publish(
            e, 
            request.FireAndForgetEvents,
            request.ParallelEvents,
            cancellationToken
        );
        return new MyMessageResponse("RESPONSE: " + request.Arg);
    }
}