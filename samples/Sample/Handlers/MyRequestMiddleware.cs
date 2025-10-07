using System.Diagnostics;
using Sample.Contracts;

namespace Sample.Handlers;


[SingletonMediatorMiddleware]
public class MyRequestMiddleware(AppSqliteConnection conn) : IRequestMiddleware<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Process(
        IMediatorContext context, 
        RequestHandlerDelegate<MyMessageResponse> next,
        CancellationToken cancellationToken
    )
    {
        var sw = Stopwatch.StartNew();
        var result = await next().ConfigureAwait(false);
        sw.Stop();

        var request = (MyMessageRequest)context.Message;
        await conn.Log(
            nameof(MyRequestMiddleware), 
            new MyMessageEvent(
                request.Arg, 
                request.FireAndForgetEvents
            ), 
            sw.ElapsedMilliseconds
        );
        return result;
    }
}
