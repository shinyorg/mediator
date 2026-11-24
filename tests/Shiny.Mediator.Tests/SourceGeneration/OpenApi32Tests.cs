// the fixture's deprecated operation, parameter and property are generated [Obsolete] - that is what is under test
#pragma warning disable CS0612, CS0618
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Shiny.Impl;
using Shiny.Mediator.Http;
using Shiny.Mediator.Tests.Mocks;

namespace Shiny.Mediator.Tests.SourceGeneration;


/// <summary>
/// Runtime checks of the code generated from openapi32.json: server-sent event streams described by OpenAPI 3.2
/// itemSchema (and 3.0/3.1 schema envelopes), discriminator defaultMapping and deprecated -> [Obsolete].
/// Everything goes through the generated resolver only - no reflection fallback.
/// </summary>
public class OpenApi32Tests
{
    static readonly IJsonTypeInfoResolver GeneratedResolver = (IJsonTypeInfoResolver)typeof(OpenApi32.Tick).Assembly
        .GetType("OpenApi32.OpenApi32JsonResolver")!
        .GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static)!
        .GetValue(null)!;


    [Fact]
    public void ItemSchema_event_stream_generates_a_server_sent_events_stream_contract()
    {
        typeof(OpenApi32.GetTicksHttpRequest).GetInterfaces().ShouldContain(typeof(IStreamRequest<OpenApi32.Tick>));
        typeof(OpenApi32.GetTicksHttpRequest).GetInterfaces().ShouldContain(typeof(IServerSentEventsStream));
    }


    [Fact]
    public void Schema_envelope_event_stream_streams_the_data_type()
    {
        // 3.0/3.1 documents (ASP.NET Core 10) describe the event with schema: SseItem<int> - the data is an int
        typeof(OpenApi32.GetCounterHttpRequest).GetInterfaces().ShouldContain(typeof(IStreamRequest<int>));
        typeof(OpenApi32.GetCounterHttpRequest).GetInterfaces().ShouldContain(typeof(IServerSentEventsStream));
    }


    [Fact]
    public void An_operation_offering_json_stays_a_request()
    {
        typeof(OpenApi32.GetLatestTickHttpRequest).GetInterfaces().ShouldContain(typeof(IRequest<OpenApi32.Tick>));
        typeof(OpenApi32.GetLatestTickHttpRequest).GetInterfaces().ShouldNotContain(typeof(IServerSentEventsStream));
    }


    [Fact]
    public async Task The_generated_stream_handler_reads_each_event()
    {
        var http = new EventStreamHandler(
            "event: tick\nid: 1\ndata: {\"n\":1}\n\n" +
            "data: {\"n\":2}\n\n"
        );
        var serializer = new DefaultJsonSerializer();
        serializer.Options.TypeInfoResolverChain.Insert(0, GeneratedResolver);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Mediator:Http:*"] = "http://sse.test" })
            .Build();

        var handler = new OpenApi32.GetTicksHttpRequestHandler(new HttpHandlerServices(
            serializer,
            new FixedHttpClientFactory(http),
            [],
            null,
            configuration
        ));

        var request = new OpenApi32.GetTicksHttpRequest { Count = 2 };
        var ticks = new List<OpenApi32.Tick>();
        await foreach (var tick in handler.Handle(request, new MockMediatorContext { Message = request }, CancellationToken.None))
            ticks.Add(tick);

        ticks.Select(x => x.N).ShouldBe([1, 2]);
        http.RequestUri!.ToString().ShouldBe("http://sse.test/ticks?count=2");
    }


    [Fact]
    public void Default_mapping_takes_missing_and_unknown_discriminators()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = GeneratedResolver };
        var pets = JsonSerializer.Deserialize<List<OpenApi32.Pet>>(
            """
            [
              { "kind": "dog", "name": "Rex", "barks": true },
              { "kind": "cat", "name": "Tom" },
              { "name": "Nameless" }
            ]
            """,
            options
        )!;

        pets[0].ShouldBeOfType<OpenApi32.Dog>().Barks.ShouldBe(true);
        pets[1].ShouldBeOfType<OpenApi32.OtherPet>().Name.ShouldBe("Tom");
        pets[2].ShouldBeOfType<OpenApi32.OtherPet>().Name.ShouldBe("Nameless");
    }


    [Fact]
    public void Deprecated_operation_parameter_and_property_are_obsolete()
    {
        typeof(OpenApi32.GetLegacyPetsHttpRequest).GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull();
        typeof(OpenApi32.GetLegacyPetsHttpRequest).GetProperty("Filter")!.GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull();
        typeof(OpenApi32.GetLegacyPetsHttpRequest).GetProperty("Name")!.GetCustomAttribute<ObsoleteAttribute>().ShouldBeNull();

        typeof(OpenApi32.Tick).GetProperty("Label")!.GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull();
        typeof(OpenApi32.Tick).GetProperty("N")!.GetCustomAttribute<ObsoleteAttribute>().ShouldBeNull();
    }
}


file class FixedHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, false);
}


file class EventStreamHandler(string body) : HttpMessageHandler
{
    public Uri? RequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.RequestUri = request.RequestUri;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
        };
        return Task.FromResult(response);
    }
}
