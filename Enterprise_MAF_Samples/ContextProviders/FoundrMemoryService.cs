using Azure.AI.Projects;
using Azure.AI.Projects.Memory;
using Azure.Identity;
using System.ClientModel;

namespace Enterprise_MAF_Samples.ContextProviders
{
#pragma warning disable MAAI001
#pragma warning disable OPENAI001
#pragma warning disable AAIP001
    public class FoundrMemoryService
    {
        private readonly FoundryAISettings _settings;
        private AIProjectClient? _aiProjectClient;
        private MemoryStoreDefaultDefinition? _memoryStoreDefaultDefinition;
        public FoundrMemoryService(Action<FoundryAISettings> configureSettings)
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
            _memoryStoreDefaultDefinition = new MemoryStoreDefaultDefinition(_settings.ModelName, _settings.EmbeddingModelName)
            {
                Options = new MemoryStoreDefaultOptions(isChatSummaryEnabled: true, isUserProfileEnabled: true)
            };
        }

        public async Task<bool> CheckIfMemoryStoreAlreadyExists()
        {
            if (_aiProjectClient == null)
            {
                throw new InvalidOperationException("AIProjectClient is not initialized.");
            }
            try
            {
                var memoryStore = await _aiProjectClient.MemoryStores.GetMemoryStoreAsync(_settings.MemoryStoreName);
                Console.WriteLine($"Memory Store {memoryStore.Value.Name} already exists.");
                return true;
            }
            catch (ClientResultException ex) when (ex.Status == 404)
            {
                Console.WriteLine($"Memory Store {_settings.MemoryStoreName} does not exist.");
                return false;
            }
        }


        public async Task<MemoryStore> CreateMemoryStoreAsync()
        {
            if (_aiProjectClient == null || _memoryStoreDefaultDefinition == null)
            {
                throw new InvalidOperationException("AIProjectClient or MemoryStoreDefaultDefinition is not initialized.");
            }

            var memoryStore = await _aiProjectClient.MemoryStores.CreateMemoryStoreAsync(
                   _settings.MemoryStoreName,
                   _memoryStoreDefaultDefinition,
                   "Managed long-term store for personalization");

            Console.WriteLine($"Memory Store {memoryStore.Value.Name} created successfully.");

            return memoryStore.Value;
        }

#pragma warning restore MAAI001
#pragma warning restore OPENAI001
#pragma warning restore AAIP001
    }
}
