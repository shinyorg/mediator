using System.Diagnostics;
using Sample.Contracts;

namespace Sample.Handlers;


[SingletonMiddleware]
public class MyRequestMiddleware(AppSqliteConnection conn) : IRequestMiddleware<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Process(MyMessageRequest request, RequestHandlerDelegate<MyMessageResponse> next, IRequestHandler requestHandler, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await next().ConfigureAwait(false);
        sw.Stop();

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
