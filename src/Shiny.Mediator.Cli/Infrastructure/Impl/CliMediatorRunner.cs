namespace Shiny.Mediator.Infrastructure.Impl;


public class CliMediatorRunner(IMediator mediator) : ICliMediatorRunner
{
    public Task Execute(string[] args)
    {
        // TODO: named args
        // TODO: positional args
        // TODO: help
        throw new NotImplementedException();
    }
}