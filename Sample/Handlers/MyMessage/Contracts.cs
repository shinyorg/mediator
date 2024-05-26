namespace Sample.Handlers.MyMessage;

public record MyMessageRequest(string Arg) : IRequest<MyMessageResponse>;

public record MyMessageResponse(string Response);

public record MyMessageEvent(string Arg) : IEvent;