namespace Sample;

public record MyMessageRequest(string Arg, bool FireAndForgetEvents) : IRequest<MyMessageResponse>;

public record MyMessageResponse(string Response);

public record MyMessageEvent(string Arg, bool FireAndForgetEvents) : IEvent;