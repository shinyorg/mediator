namespace Sample.Handlers.MyMessage;


public class SingletonRequestHandler(IMediator mediator) : IRequestHandler<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Handle(MyMessageRequest request, CancellationToken cancellationToken)
    {
        // TODO: I would normally want to fire this AFTER the return though
            // this is likely why service bus frameworks have a return method
            // could have a pre/post on handlers
        await mediator.Publish(
            new MyMessageEvent(
                "EVENT: " + request.Arg,
                request.FireAndForgetEvents,
                request.ParallelEvents
            ),
            request.FireAndForgetEvents,
            request.ParallelEvents,
            cancellationToken
        );
        return new MyMessageResponse("RESPONSE: " + request.Arg);
    }
}