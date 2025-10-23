using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Benchmarks;


[SimpleJob(RunStrategy.ColdStart, iterationCount: 50)]
[MemoryDiagnoser, StdDevColumn]
public class MediatorBenchmarks
{
    IMediator mediator = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddLogging(x => x.AddDebug());
        services.AddShinyMediator(
            x =>
            {
                x.Services.AddMediatorRegistry();
                x.Services.AddSingleton<IRequestHandler<NormalRequest, int>, NormalRequestHandler>();
            }, 
            false
        );
        var sp = services.BuildServiceProvider();
        this.mediator = sp.GetRequiredService<IMediator>();    
    }
    
    [Benchmark(Baseline = true)]
    public Task ReflectionExecution() => this.mediator.Request(new NormalRequest());
    
    [Benchmark]
    public Task SourceGenExecution() => this.mediator.Request(new FastRequest());
}