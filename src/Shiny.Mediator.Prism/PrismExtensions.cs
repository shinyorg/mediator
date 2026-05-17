using Shiny.Mediator.Prism;
using Shiny.Mediator.Prism.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// <see cref="ShinyMediatorBuilder"/> extensions that wire Prism navigation, region navigation,
/// and region-based event collection into the mediator pipeline.
/// </summary>
public static class PrismExtensions
{
    /// <summary>
    /// Registers Prism integration with the mediator: a region-based
    /// <see cref="Shiny.Mediator.Prism.Infrastructure.PrismRegionEventCollector"/>, open-generic
    /// command handlers for <see cref="IPrismNavigationCommand"/> and
    /// <see cref="IPrismRegionNavigationCommand"/>, and the <see cref="Shiny.Mediator.Prism.IGlobalNavigationService"/> singleton.
    /// </summary>
    public static ShinyMediatorBuilder AddPrismSupport(this ShinyMediatorBuilder cfg)
    {
        // cfg.Services.AddSingletonAsImplementedInterfaces<PrismNavigationRequestHandler>();
	    cfg.AddEventCollector<PrismRegionEventCollector>();
        cfg.Services.AddSingleton(typeof(ICommandHandler<>), typeof(PrismNavigationCommandHandler<>));
        cfg.Services.AddSingleton(typeof(ICommandHandler<>), typeof(PrismRegionNavigationCommandHandler<>));
        cfg.Services.AddSingleton<IGlobalNavigationService, GlobalNavigationService>();
        return cfg;
    }    
}