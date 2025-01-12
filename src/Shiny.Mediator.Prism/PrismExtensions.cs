using Shiny.Mediator.Prism;
using Shiny.Mediator.Prism.Infrastructure;

namespace Shiny.Mediator;


public static class PrismExtensions
{
    public static ShinyConfigurator AddPrismSupport(this ShinyConfigurator cfg)
    {
        // cfg.Services.AddSingletonAsImplementedInterfaces<PrismNavigationRequestHandler>();
        cfg.Services.AddSingleton(typeof(ICommandHandler<>), typeof(PrismNavigationCommandHandler<>));
        cfg.Services.AddSingleton<IGlobalNavigationService, GlobalNavigationService>();
        return cfg;
    }    
}