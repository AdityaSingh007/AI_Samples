using CopiloKit_MAF_Backend_Agent.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;

namespace CopiloKit_MAF_Backend_Agent.ContextHistoryProvider
{
    internal class AgentCosmosChatHistoryProvider : IAgentCosmosChatHistoryProvider
    {
        private readonly IConfiguration configuration;
        private const string CosmosDatabaseName = "CosmosDBForMyAIAgent";
        private const string CosmosContainerName = "ChatHistory";

        public AgentCosmosChatHistoryProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<CosmosChatHistoryProvider> GetCosmosChatHistoryProviderAsync()
        {
            var cosmosClient = await GetCosmosClientAsync();
            var cosmosDatabase = await GetCosmosDatabase(cosmosClient, CosmosDatabaseName);
            var container = await GetCosmosContainer(cosmosClient, cosmosDatabase, CosmosContainerName);

            var tenantId = configuration["TENANT_ID"] ?? throw new ArgumentNullException("TENANT_ID");
            var userId = configuration["USER_ID"] ?? throw new ArgumentNullException("USER_ID");
            var conversationId = configuration["CONVERSATION_ID"] ?? throw new ArgumentNullException("CONVERSATION_ID");

            return new CosmosChatHistoryProvider(
                cosmosClient: cosmosClient,
                databaseId: cosmosDatabase.Id,
                containerId: container.Id,
                stateInitializer: _ => new CosmosChatHistoryProvider.State(
                    conversationId: conversationId,
                    tenantId: tenantId,
                    userId: userId
                    ),
                ownsClient: false
                );
        }

        private async Task<Database> GetCosmosDatabase(CosmosClient cosmosClient, string databaseName)
        {
            var database = cosmosClient.GetDatabase(databaseName);
            return database;
        }

        private async Task<Container> GetCosmosContainer(CosmosClient cosmosClient, Database database, string containerName)
        {
            var container = database.GetContainer(containerName);
            return container;
        }

        private async Task<CosmosClient> GetCosmosClientAsync()
        {
            if (configuration == null)
            {
                throw new InvalidOperationException("Configuration is not initialized.");
            }

            string cosmosEndpoint = configuration["COSMOS_ENDPOINT"] ?? string.Empty;
            string cosmosKey = configuration["COSMOS_KEY"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cosmosEndpoint) || string.IsNullOrWhiteSpace(cosmosKey))
            {
                throw new InvalidOperationException("Cosmos DB endpoint or key is not set in the configuration.");
            }
            CosmosClient cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey, new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Gateway
            });
            return cosmosClient;
        }
    }
}
