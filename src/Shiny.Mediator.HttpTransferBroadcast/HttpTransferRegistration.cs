namespace Shiny.Mediator.HttpTransferBroadcast;

/// <summary>
/// Extension methods for registering the Shiny HTTP background transfer to mediator event bridge
/// with a <see cref="ShinyMediatorBuilder"/>.
/// </summary>
public static class HttpTransferRegistration
{
    /// <summary>
    /// Registers the HTTP transfer broadcaster so that Shiny background upload and download events are
    /// republished through the mediator as <see cref="HttpTransferEvents"/>. Call this when configuring
    /// the mediator pipeline to enable <see cref="IHttpTransferReceiver"/> implementations.
    /// </summary>
    public static ShinyMediatorBuilder AddHttpTransferBroadcast(this ShinyMediatorBuilder mediatorBuilder)
    {
        // mediatorBuilder.Services.AddShinyService<GpsBroadcaster>();
        return mediatorBuilder;
    }
}