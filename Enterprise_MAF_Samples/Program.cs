using Azure.AI.OpenAI;
using Enterprise_MAF_Samples.ContextProviders;
using Enterprise_MAF_Samples.Models;
using Enterprise_MAF_Samples.WorkFlowUtils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var gptModelConfiguration = new GptModelConfiguration();
configuration.GetSection("gptModel").Bind(gptModelConfiguration);

var gpt_4_1_Token = Environment.GetEnvironmentVariable("gpt_4_1_Mini_Token", EnvironmentVariableTarget.User);

if (string.IsNullOrEmpty(gpt_4_1_Token))
{
    Console.WriteLine("GPT-4.1 token is not configured in environment variables.");
    return;
}

if (gptModelConfiguration is null)
{
    Console.WriteLine("Failed to load GPT model configuration.");
    return;
}

if (string.IsNullOrEmpty(gptModelConfiguration.FoundryDeploymentUrl))
{
    Console.WriteLine("Foundry Deployment URL is not configured.");
    return;
}

gptModelConfiguration.ModelToken = gpt_4_1_Token;

AzureOpenAIClient client = new(new Uri(gptModelConfiguration.FoundryDeploymentUrl),
    new System.ClientModel.ApiKeyCredential(gptModelConfiguration.ModelToken));

AIAgent agent = client
    .GetChatClient("gpt-4.1-mini")
    .AsIChatClient()
    .AsAIAgent(
    new ChatClientAgentOptions()
    {
        ChatOptions = new() { Instructions = "You are a friendly assistant. Always address the user by their name." },
        AIContextProviders = [new UserInfoMemory(client.GetChatClient("gpt-4.1-mini").AsIChatClient())]
    }
    );

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    if (string.IsNullOrEmpty(input))
    {
        Console.WriteLine("Input cannot be empty. Please enter a valid prompt.");
        continue;
    }
    Console.WriteLine("\n=== Streaming AI Response ===");
    await foreach (var update in agent.RunStreamingAsync(input, session))
    {
        Console.Write(update);
    }
    Console.WriteLine();
}

//await HITLLoop.Main();









