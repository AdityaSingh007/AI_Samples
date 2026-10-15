using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Enterprise_MAF_Samples.Providers;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;


namespace Enterprise_MAF_Samples.ContextProviders
{
    public class AgentWithFoundryMemory
    {
#pragma warning disable MAAI001
#pragma warning disable OPENAI001

        private readonly FoundryAISettings _settings;
        private AIProjectClient? _aiProjectClient;

        public AgentWithFoundryMemory(Action<FoundryAISettings> configureSettings)
        {
            ArgumentNullException.ThrowIfNull(configureSettings);
            _settings = new FoundryAISettings();
            configureSettings(_settings);

            ArgumentNullException.ThrowIfNull(_settings.TenantId);
            ArgumentNullException.ThrowIfNull(_settings.ClientId);
            ArgumentNullException.ThrowIfNull(_settings.ClientSecret);
            ArgumentNullException.ThrowIfNull(_settings.FoundryProjectUrl);
            ArgumentNullException.ThrowIfNull(_settings.FoundryModelDeploymentUrl);
            ArgumentNullException.ThrowIfNull(_settings.MemoryStoreName);
            ArgumentNullException.ThrowIfNull(_settings.Scope);
            ArgumentNullException.ThrowIfNull(_settings.ModelToken);
            ArgumentNullException.ThrowIfNull(_settings.ModelName);

            var credential = new ClientSecretCredential(_settings.TenantId, _settings.ClientId, _settings.ClientSecret);
            _aiProjectClient = new AIProjectClient(new Uri(_settings.FoundryProjectUrl), credential);
        }

        public async Task<FoundryMemoryProvider> GetFoundryMemoryProviderAsync()
        {
            if (_aiProjectClient == null)
            {
                throw new InvalidOperationException("AIProjectClient is not initialized.");
            }

            var foundryMemoryProvider = new FoundryMemoryProvider(_aiProjectClient, _settings.MemoryStoreName,
                stateInitializer: _ => new(new FoundryMemoryProviderScope(_settings.Scope)));

            return foundryMemoryProvider;
        }

        private async Task<AIAgent> CreateAgentAsync(bool isLocalMemory = false)
        {
            var openAIClient = await GetAzureOpenAIClientAsync();
            AIContextProvider memoryProvider;
            if (isLocalMemory)
            {
                memoryProvider = new LocalMemoryProvider("Data Source=Data/LocalMemory.db",
                    openAIClient.GetChatClient(_settings.ModelName).AsIChatClient(),
                    openAIClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator());
            }
            else
            {
                memoryProvider = await GetFoundryMemoryProviderAsync();
            }
            var chatClient = openAIClient.GetChatClient(_settings.ModelName);
            AIAgent agent = chatClient
                             .AsIChatClient()
                             .AsAIAgent(new ChatClientAgentOptions()
                             {
                                 Name = "Agent powered by Foundry Memory",
                                 ChatOptions = new ChatOptions
                                 {
                                     Instructions = "You are a helpful assistant that always responds in English."
                                 },
                                 AIContextProviders = [memoryProvider]
                             });
            return agent;

        }

        public async Task RunAsync(bool isLocalMemory = false)
        {
            var _mafAgent = await CreateAgentAsync(isLocalMemory);
            FoundryMemoryProvider? foundryMemoryProvider = null;
            AgentSession? agentSession = await _mafAgent.CreateSessionAsync();
            if (!isLocalMemory)
            {
                foundryMemoryProvider = _mafAgent.GetService<FoundryMemoryProvider>();
            }
            else 
            {
                agentSession.StateBag.SetValue(nameof(FoundryAISettings.Scope), _settings.Scope);
            }
            while (true)
            {
                Console.Write("User: ");
                string userInput = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine("Exiting...");
                    break;
                }
                await foreach (var update in _mafAgent.RunStreamingAsync(userInput, agentSession))
                {
                    Console.Write(update);
                }

                if (!isLocalMemory)
                {
                    await foundryMemoryProvider!.WhenUpdatesCompletedAsync(pollingInterval: TimeSpan.FromSeconds(1));
                }
                Console.WriteLine();
            }
        }

        private async Task<AzureOpenAIClient> GetAzureOpenAIClientAsync()
        {
            AzureOpenAIClient client = new(new Uri(_settings.FoundryModelDeploymentUrl),
               new System.ClientModel.ApiKeyCredential(_settings.ModelToken));
            return client;
        }

#pragma warning restore MAAI001
#pragma warning restore OPENAI001

    }
}
