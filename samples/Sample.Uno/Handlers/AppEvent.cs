using Shiny.Mediator;

namespace Sample.Handlers;

public record AppEvent(string Message) : IEvent;
