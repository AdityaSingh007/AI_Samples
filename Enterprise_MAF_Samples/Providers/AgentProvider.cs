using Azure.AI.OpenAI;
using Enterprise_MAF_Samples.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace Enterprise_MAF_Samples.Providers
{
    internal class AgentProvider
    {
        private readonly IConfigurationRoot configurationRoot;
        private readonly GptModelConfiguration gptModelConfiguration = new GptModelConfiguration();

        public AgentProvider(IConfigurationRoot configurationRoot)
        {
            this.configurationRoot = configurationRoot;
            configurationRoot.GetSection("gptModel").Bind(gptModelConfiguration);

            if (string.IsNullOrWhiteSpace(gptModelConfiguration.FoundryDeploymentUrl))
            {
                throw new ArgumentNullException(nameof(gptModelConfiguration.FoundryDeploymentUrl), "Foundry Deployment URL is not configured.");
            }

            gptModelConfiguration.ModelToken = Environment.GetEnvironmentVariable("gpt_4_1_Mini_Token", EnvironmentVariableTarget.User) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gptModelConfiguration.ModelToken))
            {
                throw new InvalidOperationException("GPT-4.1 Mini Token is not set in the environment variables.");
            }
        }

        public async Task<ChatClient> GetChatClientAsync(string deploymentName)
        {
            AzureOpenAIClient client = new(new Uri(gptModelConfiguration.FoundryDeploymentUrl),
               new System.ClientModel.ApiKeyCredential(gptModelConfiguration.ModelToken));
            return client.GetChatClient(deploymentName);
        }
        public async Task<AIAgent> CreateAgent(ChatClientAgentOptions chatClientAgentOptions)
        {
            if (configurationRoot == null)
            {
                throw new ArgumentNullException(nameof(configurationRoot));
            }

            if (gptModelConfiguration == null)
            {
                throw new InvalidOperationException("Failed to load GPT model configuration.");
            }

            if (string.IsNullOrWhiteSpace(gptModelConfiguration.ModelToken))
            {
                throw new InvalidOperationException("GPT-4.1 Mini Token is not set in the environment variables.");
            }

            if (string.IsNullOrWhiteSpace(gptModelConfiguration.FoundryDeploymentUrl))
            {
                throw new ArgumentNullException(nameof(gptModelConfiguration.FoundryDeploymentUrl), "Foundry Deployment URL is not configured.");
            }

            var client = await GetChatClientAsync("gpt-4.1-mini");

            AIAgent agent = client
                            .AsIChatClient()
                            .AsAIAgent(chatClientAgentOptions);
            return agent;
        }
    }
}
