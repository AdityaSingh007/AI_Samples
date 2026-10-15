using GitHub.Copilot;
using Microsoft.Agents.AI;

try
{
    //await RunCopilotAsync();
    await  RunCopilotAsAgentAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

static async Task RunCopilotAsAgentAsync() 
{
    await using CopilotClient copilotClient = new();
    await copilotClient.StartAsync();
    var providerConfig = new ProviderConfig
    {
        Type = "openai", // Options: "openai", "azure", "anthropic"
        BaseUrl = "https://ia.devops.lattes.se.com/v1", // Or your custom/Azure gateway endpoint
        ApiKey = "0e59c7673a7197dd6191045edfcdaf36dce2311328a6dce4eff77e01c50ebb55",
        WireApi = "completions" // "completions" or "responses" depending on target API capability
    };

    var copilotSessionConfig = new SessionConfig
    {
        Model = "Qwen/Qwen3.8-27B", // The deployment/model name on your custom provider
        Provider = providerConfig,
        OnPermissionRequest = PermissionHandler.ApproveAll // Auto-approve agent tools if used
    };

    AIAgent agent = copilotClient.AsAIAgent(sessionConfig: copilotSessionConfig);
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Explain the difference between a task and a thread in .NET."))
    {
        Console.Write(update);
    }

    Console.WriteLine();
}


static async Task RunCopilotAsync()
{
    using var client = new CopilotClient();
    await client.StartAsync();

    var providerConfig = new ProviderConfig
    {
        Type = "openai", // Options: "openai", "azure", "anthropic"
        BaseUrl = "https://ia.devops.lattes.se.com/v1", // Or your custom/Azure gateway endpoint
        ApiKey = "0e59c7673a7197dd6191045edfcdaf36dce2311328a6dce4eff77e01c50ebb55",
        WireApi = "completions" // "completions" or "responses" depending on target API capability
    };

    var sessionConfig = new SessionConfig
    {
        Model = "Qwen/Qwen3.8-27B", // The deployment/model name on your custom provider
        Provider = providerConfig,
        OnPermissionRequest = PermissionHandler.ApproveAll // Auto-approve agent tools if used
    };

    var session = await client.CreateSessionAsync(sessionConfig);

    var response = await session.SendAndWaitAsync(new MessageOptions
    {
        Prompt = "Explain the difference between a task and a thread in .NET."
    });

    Console.WriteLine(response.Data.Content);

    await client.StopAsync();
}
