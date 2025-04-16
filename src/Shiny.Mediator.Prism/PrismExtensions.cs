using Shiny.Mediator.Prism;
using Shiny.Mediator.Prism.Infrastructure;

namespace Shiny.Mediator;


public static class PrismExtensions
{
    public static ShinyMediatorBuilder AddPrismSupport(this ShinyMediatorBuilder cfg)
    {
        // cfg.Services.AddSingletonAsImplementedInterfaces<PrismNavigationRequestHandler>();
	    cfg.AddEventCollector<PrismRegionEventCollector>();
        cfg.Services.AddSingleton(typeof(ICommandHandler<>), typeof(PrismNavigationCommandHandler<>));
        cfg.Services.AddSingleton<IGlobalNavigationService, GlobalNavigationService>();
        return cfg;
    }    
}