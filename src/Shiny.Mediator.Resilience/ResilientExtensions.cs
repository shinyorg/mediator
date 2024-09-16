using Polly;
using Shiny.Mediator.Resilience.Handlers;

namespace Shiny.Mediator;


public static class ResilientExtensions
{
    public static ShinyConfigurator AddResiliencyMiddleware(this ShinyConfigurator configurator, params (string Key, Action<ResiliencePipelineBuilder> Builder)[] rbuilders)
    {
        foreach (var rbs in rbuilders)
            configurator.Services.AddResiliencePipeline(rbs.Key.ToLower(), builder => rbs.Builder.Invoke(builder));

        configurator.AddOpenRequestMiddleware(typeof(ResilientRequestHandlerMiddleware<,>));
        return configurator;
    }
}