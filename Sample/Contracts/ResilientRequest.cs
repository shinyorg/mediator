namespace Sample.Contracts;

[Resilient("Test")]
public record ResilientRequest : IRequest<string>;