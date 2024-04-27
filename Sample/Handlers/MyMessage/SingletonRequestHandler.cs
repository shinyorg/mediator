

namespace Sample.Handlers.MyMessage;


public class SingletonRequestHandler : IRequestHandler<MyMessageRequest, MyMessageResponse>
{
    public Task<MyMessageResponse> Handle(MyMessageRequest command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}