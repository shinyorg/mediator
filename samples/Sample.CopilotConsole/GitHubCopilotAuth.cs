using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Sample.CopilotConsole;

public static class GitHubCopilotAuth
{
    // GitHub Copilot's well-known OAuth client ID (used by VS Code)
    const string ClientId = "Iv1.b507a08c87ecfe98";
    const string DeviceCodeUrl = "https://github.com/login/device/code";
    const string TokenUrl = "https://github.com/login/oauth/access_token";
    const string CopilotTokenUrl = "https://api.github.com/copilot_internal/v2/token";

    static readonly string TokenFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".shiny-mediator-copilot-token"
    );

    /// <summary>
    /// Gets a valid Copilot session token, performing device flow auth if needed.
    /// Returns the Copilot API token and endpoint.
    /// </summary>
    public static async Task<CopilotSession> GetSessionAsync(HttpClient http, CancellationToken ct = default)
    {
        var githubToken = await GetGitHubTokenAsync(http, ct);
        return await ExchangeForCopilotTokenAsync(http, githubToken, ct);
    }

    static async Task<string> GetGitHubTokenAsync(HttpClient http, CancellationToken ct)
    {
        // Try to load saved token
        if (File.Exists(TokenFilePath))
        {
            var saved = await File.ReadAllTextAsync(TokenFilePath, ct);
            if (!string.IsNullOrWhiteSpace(saved))
            {
                // Validate the token is still good
                if (await ValidateGitHubTokenAsync(http, saved.Trim(), ct))
                    return saved.Trim();

                Console.WriteLine("Saved token is expired, re-authenticating...");
            }
        }

        // Perform device flow
        var token = await DeviceFlowAuthAsync(http, ct);

        // Save for next time
        await File.WriteAllTextAsync(TokenFilePath, token, ct);
        return token;
    }

    static async Task<bool> ValidateGitHubTokenAsync(HttpClient http, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("ShinyMediatorSample/1.0");

        try
        {
            using var response = await http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    static async Task<string> DeviceFlowAuthAsync(HttpClient http, CancellationToken ct)
    {
        // Step 1: Request device code
        using var codeRequest = new HttpRequestMessage(HttpMethod.Post, DeviceCodeUrl);
        codeRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        codeRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["scope"] = "read:user"
        });

        using var codeResponse = await http.SendAsync(codeRequest, ct);
        codeResponse.EnsureSuccessStatusCode();
        var deviceCode = await codeResponse.Content.ReadFromJsonAsync<DeviceCodeResponse>(ct)
            ?? throw new InvalidOperationException("Failed to get device code");

        Console.WriteLine();
        Console.WriteLine("=== GitHub Copilot Authentication ===");
        Console.WriteLine($"Please open: {deviceCode.VerificationUri}");
        Console.WriteLine($"Enter code:  {deviceCode.UserCode}");
        Console.WriteLine();
        Console.WriteLine("Waiting for authorization...");

        // Step 2: Poll for token
        var interval = Math.Max(deviceCode.Interval, 5);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(interval), ct);

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
            tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["device_code"] = deviceCode.DeviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            });

            using var tokenResponse = await http.SendAsync(tokenRequest, ct);
            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(ct);

            if (tokenResult?.AccessToken is not null)
            {
                Console.WriteLine("Authenticated successfully!");
                return tokenResult.AccessToken;
            }

            if (tokenResult?.Error == "slow_down")
            {
                interval += 5;
            }
            else if (tokenResult?.Error != "authorization_pending")
            {
                throw new InvalidOperationException(
                    $"Authentication failed: {tokenResult?.Error} - {tokenResult?.ErrorDescription}");
            }
        }
    }

    static async Task<CopilotSession> ExchangeForCopilotTokenAsync(
        HttpClient http, string githubToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, CopilotTokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
        request.Headers.UserAgent.ParseAdd("ShinyMediatorSample/1.0");

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<CopilotTokenResponse>(ct)
            ?? throw new InvalidOperationException("Failed to get Copilot token");

        return new CopilotSession(session.Token, session.Endpoints.Api);
    }

    record DeviceCodeResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; init; } = "";

        [JsonPropertyName("user_code")]
        public string UserCode { get; init; } = "";

        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; init; } = "";

        [JsonPropertyName("interval")]
        public int Interval { get; init; }
    }

    record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }

    record CopilotTokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; init; } = "";

        [JsonPropertyName("endpoints")]
        public CopilotEndpoints Endpoints { get; init; } = new();
    }

    record CopilotEndpoints
    {
        [JsonPropertyName("api")]
        public string Api { get; init; } = "";
    }
}

public record CopilotSession(string Token, string Endpoint);
