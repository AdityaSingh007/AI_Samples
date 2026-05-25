using CAE_AI_Samples.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace CAE_AI_Samples.Executors
{
    internal sealed partial class AuthenticateExecutor(HttpClient httpClient, IMemoryCache memoryCache) : Executor<UserCredentials, string>("AuthenticateExecutor")
    {
        private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        private readonly IMemoryCache memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        private readonly string authenticateEndpoint = "Authentication/login";
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        [MessageHandler]
        public override async ValueTask<string> HandleAsync(UserCredentials message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var response = await PostTokenRequestAsync(authenticateEndpoint, message);
            response.EnsureSuccessStatusCode();
            var tokenData = await ReadTokenResponseAsync(response, "Failed to retrieve token.");

            memoryCache.Set("AuthToken", tokenData.AccessToken);
            memoryCache.Set("RefreshToken", tokenData.RefreshToken);
            memoryCache.Set("TokenExpiration", DateTime.UtcNow);
            memoryCache.Set("UserName", message.UserName);
            memoryCache.Set("IsAuthenticated", true);
            memoryCache.Set("Password", message.Password);

            await context.QueueStateUpdateAsync(
                           key: "AuthToken",
                           value: tokenData.AccessToken,
                           scopeName: "Security",
                           cancellationToken: cancellationToken);

            ConsoleUi.WriteSuccess("User authenticated.");

            return "Authenticate executor completed";
        }

        private async Task<HttpResponseMessage> PostTokenRequestAsync(string url, UserCredentials requestBody)
        {
            var json = JsonSerializer.Serialize(requestBody, jsonSerializerOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(url, content);
        }

        public static async Task<TokenResponse> ReadTokenResponseAsync(HttpResponseMessage response, string errorMessage)
        {
            var responseJson = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<TokenResponse>(responseJson, jsonSerializerOptions);

            if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
            {
                throw new Exception(errorMessage);
            }

            return tokenData;
        }
    }
}
