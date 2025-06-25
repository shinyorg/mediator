using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.Http;


/*
// put/post body are named the same as the overall request object when a pre/post fix is used
[global::Shiny.Mediator.Http.HttpAttribute(global::Shiny.Mediator.Http.HttpVerb.Post, "/teams")]
public partial class CreateTeam : global::Shiny.Mediator.Http.IHttpRequest<global::ShinyScoreboard.ValidateResult>
{
   [global::Shiny.Mediator.Http.HttpParameter(global::Shiny.Mediator.Http.HttpParameterType.Body)]
   public global::ShinyScoreboard.CreateTeam Body { get; set; }
}
 */
public class OpenApiContractGeneratorTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, false)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", true, false)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, true)]
    [InlineData("./Http/enums.json", null, "TestApi", false, false)]
    [InlineData("./Http/validateresult.json", null, "TestApi", false, false)]
    public Task Local_Tests(
        string path, 
        string? contractPostfix, 
        string nameSpace, 
        bool useInternalClasses,
        bool generateModelsOnly
    )
    { 
        using var doc = File.OpenRead(path);
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = nameSpace,
                ContractPostfix = contractPostfix,
                UseInternalClasses = useInternalClasses,
                GenerateModelsOnly = generateModelsOnly
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(doc);
        
        return Verify(content)
            .UseParameters(
                path, 
                contractPostfix, 
                nameSpace, 
                useInternalClasses, 
                generateModelsOnly
            );
    }
    
    
    [Theory]
    [InlineData("https://api.themeparks.wiki/docs/v1.yaml", "ThemeParksApi")]
    public async Task RemoteTests(string uri, string nameSpace)
    {
        var http = new HttpClient();
        var stream = await http.GetStreamAsync(uri);
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = nameSpace,
                ContractPostfix = "HttpRequest"
            }, 
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(stream);
    
        await Verify(content).UseParameters(uri, nameSpace);
    }
}