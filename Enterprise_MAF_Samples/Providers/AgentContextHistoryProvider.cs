using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Enterprise_MAF_Samples.Providers
{
    internal class AgentContextHistoryProvider
    {
        private readonly IConfigurationRoot configurationRoot;
        private const string CosmosDatabaseName = "CosmosDBForMyAIAgent";
        private const string CosmosContainerName = "ChatHistory";

        public AgentContextHistoryProvider(IConfigurationRoot configurationRoot)
        {
            this.configurationRoot = configurationRoot;
        }

        public async Task<CosmosChatHistoryProvider> GetCosmosChatHistoryProviderAsync()
        {
            var cosmosClient = await GetCosmosClientAsync();
            var cosmosDatabase = await GetCosmosDatabase(cosmosClient, CosmosDatabaseName);
            var container = await GetCosmosContainer(cosmosClient, cosmosDatabase, CosmosContainerName);

            var tenantId = configurationRoot["TENANT_ID"] ?? throw new ArgumentNullException("TENANT_ID");
            var userId = configurationRoot["USER_ID"] ?? throw new ArgumentNullException("USER_ID");
            var conversationId = configurationRoot["CONVERSATION_ID"] ?? throw new ArgumentNullException("CONVERSATION_ID");

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
            if (configurationRoot == null)
            {
                throw new InvalidOperationException("Configuration is not initialized.");
            }

            string cosmosEndpoint = configurationRoot["COSMOS_ENDPOINT"] ?? string.Empty;
            string cosmosKey = configurationRoot["COSMOS_KEY"] ?? string.Empty;
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
