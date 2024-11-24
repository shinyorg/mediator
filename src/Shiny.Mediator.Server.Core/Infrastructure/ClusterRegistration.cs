namespace Shiny.Mediator.Server.Infrastructure;

public record ClusterRegistration(
    string ClusterName,
    string[] HandledRequestTypes,
    string[] SubscribedEventTypes
);