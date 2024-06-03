namespace Sample;

// [RegisterMiddleware]
public class MyRequestMiddleware : IRequestMiddleware<MyMessageRequest, MyMessageResponse>
{
    public async Task<MyMessageResponse> Process(MyMessageRequest request, Func<Task<MyMessageResponse>> next, CancellationToken cancellationToken)
    {
        // BEFORE - If connected - pull from API after save to cache ELSE pull from cache 
        var result = await next();
        // AFTER = cache
        return result;
    }
}

public class CatchAllRequestMiddleware : IRequestMiddleware<IRequest<object>, object> 
{
    public Task<object> Process(IRequest<object> request, Func<Task<object>> next, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}