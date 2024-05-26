namespace Sample.Handlers.MyMessage;


public class SingletonRequestHandler(IMediator mediator) : IRequestHandler<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Handle(MyMessageRequest command, CancellationToken cancellationToken)
    {
        // TODO: I would normally want to fire this AFTER the return though
            // this is likely why service bus frameworks have a return method
            // could have a pre/post on handlers
        await mediator.Publish(
            new MyMessageEvent("This is my message"),
            fireAndForget: true,
            executeInParallel: true,
            cancellationToken
        );
        return new MyMessageResponse("RESPONSE: " + command.Arg);
    }
}