namespace Shiny.Mediator;

public interface IEvent { }

/// <summary>
/// This is a good base type if you want to make use of covariance in your handlers
/// </summary>
public record Event : IEvent;