using System.Diagnostics;
using Sample.Contracts;

namespace Sample.Handlers;


[SingletonMiddleware]
public class MyRequestMiddleware(AppSqliteConnection conn) : IRequestMiddleware<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Process(
        ExecutionContext<MyMessageRequest> context, 
        RequestHandlerDelegate<MyMessageResponse> next 
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
