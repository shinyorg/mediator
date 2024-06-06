using System.Diagnostics;

namespace Sample;


// [RegisterMiddleware]
public class MyRequestMiddleware(AppSqliteConnection conn) : IRequestMiddleware<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Process(MyMessageRequest request, RequestHandlerDelegate<MyMessageResponse> next, IRequestHandler<MyMessageRequest, MyMessageResponse> requestHandler, CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();
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
