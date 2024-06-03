namespace Sample;


// [RegisterHandler]
public class SingletonRequestHandler(IMediator mediator, AppSqliteConnection data) : IRequestHandler<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Handle(MyMessageRequest request, CancellationToken cancellationToken)
    {
        // TODO: I would normally want to fire this AFTER the return though
            // this is likely why service bus frameworks have a return method
            // could have a pre/post on handlers
        var e = new MyMessageEvent(
            request.Arg,
            request.FireAndForgetEvents
        );
        await data.Log("SingletonRequestHandler", e);
        if (request.FireAndForgetEvents)
        {
            mediator.Publish(e).RunInBackground(ex =>
            {
                // TODO: log this
            });
        }
        else
        {
            await mediator.Publish(
                e,
                cancellationToken
            );
        }

        return new MyMessageResponse("RESPONSE: " + request.Arg);
    }
}