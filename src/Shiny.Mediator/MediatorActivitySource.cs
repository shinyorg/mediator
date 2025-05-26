using System.Diagnostics;

namespace Shiny.Mediator;


public static class MediatorActivitySource
{
    public static ActivitySource Value { get; set; } = new("shiny.mediator");
}