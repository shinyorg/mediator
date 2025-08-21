namespace Shiny.Mediator.Infrastructure;

public interface ICliMediatorRunner
{
    Task Execute(string[] args);
}