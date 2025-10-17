using Sample.Contracts;

namespace Sample.Handlers;


[MediatorSingleton]
public partial class SingletonRequestHandler(AppSqliteConnection data) : IRequestHandler<MyMessageRequest, MyMessageResponse>
{
    // [Cache(Storage = StoreType.File, MaxAgeSeconds = 30, OnlyForOffline = true)]
    [OfflineAvailable]
    public async Task<MyMessageResponse> Handle(MyMessageRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var e = new MyMessageEvent(
            request.Arg,
            request.FireAndForgetEvents
        );
        await data.Log("SingletonRequestHandler", e);
        if (request.FireAndForgetEvents)
        {
            context.Publish(e).RunInBackground(ex =>
            {
                // log this or something
                Console.WriteLine(ex);
            });
        }
        else
        {
            await context.Publish(
                e,
                true,
                cancellationToken
            );
        }

        return new MyMessageResponse("RESPONSE: " + request.Arg);
    }
}