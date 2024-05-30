namespace Shiny.Mediator.Tests;

public record TestRequestNoResponse : IRequest;

public record TestRequest : IRequest<string>;

public record TestEvent : IEvent;