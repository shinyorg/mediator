using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using Sample.CopilotConsole;
using Shiny.Mediator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddShinyMediator(cfg =>
{
    cfg.AddMediatorRegistry();
    cfg.AddGeneratedAITools();
});

var host = builder.Build();

// Authenticate with GitHub Copilot
using var http = new HttpClient();
Console.WriteLine("Authenticating with GitHub Copilot...");
var session = await GitHubCopilotAuth.GetSessionAsync(http);

// Create a handler that injects the Copilot token and required headers
var transport = new CopilotTokenHandler(session.Token)
{
    InnerHandler = new HttpClientHandler()
};

var openAiClient = new OpenAIClient(
    new ApiKeyCredential("copilot-placeholder"),
    new OpenAIClientOptions
    {
        Transport = new HttpClientPipelineTransport(new HttpClient(transport)),
        Endpoint = new Uri("https://api.githubcopilot.com")
    }
);

IChatClient chatClient = openAiClient
    .GetChatClient("gpt-4.1")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

// Get AI tools from mediator
var tools = host.Services.GetServices<AITool>().ToList();

Console.WriteLine();
Console.WriteLine($"Registered {tools.Count} mediator AI tool(s):");
foreach (var tool in tools)
    Console.WriteLine($"  - {tool.Name}: {tool.Description}");

Console.WriteLine();
Console.WriteLine("Chat with GitHub Copilot (type 'exit' to quit)");
Console.WriteLine("The AI can use mediator tools: weather, datetime, calculator");
Console.WriteLine(new string('-', 60));

var history = new List<ChatMessage>();
var options = new ChatOptions
{
    Tools = tools
};

while (true)
{
    Console.Write("\nYou: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    history.Add(new ChatMessage(ChatRole.User, input));

    try
    {
        var response = await chatClient.GetResponseAsync(history, options);

        // Add all response messages to history (including tool calls/results)
        history.AddRange(response.Messages);

        // Print the final text response
        var text = response.Text;
        if (!string.IsNullOrWhiteSpace(text))
            Console.WriteLine($"\nCopilot: {text}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError: {ex.Message}");
    }
}

Console.WriteLine("\nGoodbye!");
