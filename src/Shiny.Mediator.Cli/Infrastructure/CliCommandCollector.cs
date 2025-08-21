namespace Shiny.Mediator.Infrastructure;

public class CliCommandCollector
{
    readonly Dictionary<string, Type> routes = new();
    public IReadOnlyDictionary<string, Type> Routes => this.routes;

    public void Add(string route, Type handlerType) => this.routes.Add(route, handlerType);
}