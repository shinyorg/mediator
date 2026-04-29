using System.Net.Http.Headers;

namespace Sample.CopilotConsole;

public class CopilotTokenHandler(string token) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("Copilot-Integration-Id", "vscode-chat");
        return base.SendAsync(request, cancellationToken);
    }
}
