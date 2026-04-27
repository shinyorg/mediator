namespace Shiny.Mediator.HttpTransferBroadcast;

public static class HttpTransferRegistration
{
    public static ShinyMediatorBuilder AddHttpTransferBroadcast(this ShinyMediatorBuilder mediatorBuilder)
    {
        // mediatorBuilder.Services.AddShinyService<GpsBroadcaster>();
        return mediatorBuilder;
    }
}