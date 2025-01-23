using System.Diagnostics;
using Sample.Contracts;

namespace Sample.Handlers;


public class MyRequestMiddleware(AppSqliteConnection conn) : IRequestMiddleware<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Process(
        RequestContext<MyMessageRequest> context, 
        RequestHandlerDelegate<MyMessageResponse> next,
        CancellationToken cancellationToken
    )
    {
        var sw = Stopwatch.StartNew();
        var result = await next().ConfigureAwait(false);
        sw.Stop();

        await conn.Log(
            nameof(MyRequestMiddleware), 
            new MyMessageEvent(
                context.Request.Arg, 
                context.Request.FireAndForgetEvents
            ), 
            sw.ElapsedMilliseconds
        );
        return result;
    }
}
