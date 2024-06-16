using Polly;

namespace Shiny.Mediator.Resilience;


public static class ResilientExtensions
{
    public static ShinyConfigurator AddResiliencyMiddleware(this ShinyConfigurator configurator, (string Key, Action<ResiliencePipelineBuilder> Builder)[] rbuilders)
    {
        foreach (var rbs in rbuilders)
            configurator.Services.AddResiliencePipeline(rbs.Key, builder => rbs.Builder.Invoke(builder));

        configurator.AddOpenRequestMiddleware(typeof(ResilientRequestHandlerMiddleware<,>));
        return configurator;
    }
}