namespace Sample.Contracts;

public record TickerRequest(int Repeat, int Multiplier, int GapSeconds) : IStreamRequest<string>;