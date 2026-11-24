namespace Sample.Contracts;

/// <summary>
/// Navigation as a plain mediator command - see MyNavCommandHandler
/// </summary>
public record MyNavCommand(string? Arg) : Shiny.Mediator.ICommand;
