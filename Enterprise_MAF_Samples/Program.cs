using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true, reloadOnChange: true)
    .Build();
try
{
    #region Configuration
    string projectEndpoint = configuration["FoundryProjectEndpoint"] ?? throw new ArgumentNullException("ProjectEndpoint is not configured.");
    string chatModel = configuration["ChatModel"] ?? "gpt-4.1-mini";
    string embeddingModel = configuration["EmbeddingModel"] ?? "text-embedding-3-small";
    var clinet_Id = configuration["CLIENT_ID"] ?? throw new ArgumentNullException("ClientId is not configured.");
    var tenant_Id = configuration["TENANT_ID"] ?? throw new ArgumentNullException("TenantId is not configured.");
    var client_secret = configuration["CLIENT_SECRET"] ?? throw new ArgumentNullException("ClientSecret is not configured.");
    var gpt_4_1_Token = Environment.GetEnvironmentVariable("gpt_4_1_Mini_Token") ?? throw new ArgumentNullException("GPT-4.1 token is not configured.");
    var foundry_Model_Deployment_Url = configuration["Foundry_Model_Deployment_Url"] ?? throw new ArgumentNullException("FoundryModelDeploymentUrl is not configured.");
    var scope = configuration["Scope"];
    ArgumentException.ThrowIfNullOrEmpty(scope);
    var memoryStoreName = configuration["MemoryStoreName"];
    ArgumentException.ThrowIfNullOrEmpty(memoryStoreName);
    var useLocalMemory = bool.Parse(configuration["USE_LOCAL_MEMORY"] ?? "false");
    #endregion

    #region Implementation
    if (!useLocalMemory)
    {
        var foundryMemoryService = new Enterprise_MAF_Samples.ContextProviders.FoundrMemoryService(settings =>
        {
            settings.TenantId = tenant_Id;
            settings.ClientId = clinet_Id;
            settings.ClientSecret = client_secret;
            settings.FoundryProjectUrl = projectEndpoint;
            settings.Scope = scope;
            settings.MemoryStoreName = memoryStoreName;
            settings.ModelToken = gpt_4_1_Token;
            settings.ModelName = chatModel;
            settings.FoundryModelDeploymentUrl = foundry_Model_Deployment_Url;
            settings.EmbeddingModelName = embeddingModel;
        });

        if (await foundryMemoryService.CheckIfMemoryStoreAlreadyExists() == false)
        {
            await foundryMemoryService.CreateMemoryStoreAsync();
        }
    }


    var agentWithFoundryMemory = new Enterprise_MAF_Samples.ContextProviders.AgentWithFoundryMemory(
        settings =>
        {
            settings.TenantId = tenant_Id;
            settings.ClientId = clinet_Id;
            settings.ClientSecret = client_secret;
            settings.FoundryProjectUrl = projectEndpoint;
            settings.Scope = scope;
            settings.MemoryStoreName = memoryStoreName;
            settings.ModelToken = gpt_4_1_Token;
            settings.ModelName = chatModel;
            settings.FoundryModelDeploymentUrl = foundry_Model_Deployment_Url;
            settings.EmbeddingModelName = embeddingModel;
        });

    await agentWithFoundryMemory.RunAsync(isLocalMemory: useLocalMemory);
    #endregion
}
catch (Exception ex)
{
    Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
}





