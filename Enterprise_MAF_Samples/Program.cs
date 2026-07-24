using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Enterprise_MAF_Samples.ContextProviders;
using Enterprise_MAF_Samples.Models;
using Enterprise_MAF_Samples.Providers;
using Enterprise_MAF_Samples.WorkFlowUtils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true, reloadOnChange: true)
    .Build();
try
{
    var agentProvider = new AgentProvider(configuration);
    var chatClient = await agentProvider.GetChatClientAsync("gpt-4.1-mini");
    var cosmosChatHistoryProvider = await new AgentContextHistoryProvider(configuration).GetCosmosChatHistoryProviderAsync();

#pragma warning disable MAAI001 
    var compactionStrategy = new SummarizationCompactionStrategy(
        chatClient: chatClient.AsIChatClient(),
        trigger: CompactionTriggers.MessagesExceed(8)
        );
    var compactionProvider = new CompactionProvider(compactionStrategy);
#pragma warning restore MAAI001

    var agent = await agentProvider.CreateAgent(new ChatClientAgentOptions
    {
        Name = "PersonalAssistant",
        ChatOptions = new()
        {
            ModelId = "gpt-4.1-mini",
            Instructions = "You are a helpful assistant. Use known memories about the user. Always address the user by his/her name.",
        },
        ChatHistoryProvider = cosmosChatHistoryProvider,
        AIContextProviders = [compactionProvider]
    });

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
}
catch (Exception ex)
{
    Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
}

// 1. Define your connection parameters and deployment targets
//string projectEndpoint = "https://singhadi041-6488-resource.services.ai.azure.com/api/projects/singhadi041-6488";
//string chatModelDeployment = "gpt-4.1-mini";
//string embeddingModelDeployment = "text-embedding-3-small";
//string memoryStoreName = "customer-long-term-vault";
//var credential = new ClientSecretCredential("71f2e7b5-db7d-4db7-9b7d-dc4fbc5cb0aa", 
//                                            "cbfb1eea-1ccc-4505-b775-efb2d2c19bdd", 
//                                            "");
//var projectClient = new AIProjectClient
//                    (
//                    new Uri(projectEndpoint),
//                    tokenProvider: credential
//                    );

//OpenAIClient openAIClient = projectClient.GetProjectOpenAIClient();


//#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//FoundryMemoryProvider memoryProvider = new(
//            projectClient,
//            memoryStoreName: memoryStoreName,
//            stateInitializer: _ => new(new FoundryMemoryProviderScope("user-taylor-123"))
//        );
//#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.



//ChatClientAgent agent = projectClient.AsAIAgent(new ChatClientAgentOptions
//{
//    Name = "PersonalTravelAssistant",
//    ChatOptions = new()
//    {
//        ModelId = chatModelDeployment,
//        Instructions = "You are a helpful assistant. Use known memories about the user.",
//    },
//    AIContextProviders = [memoryProvider], // Mounts memory pipeline to the agent context
//});

//await memoryProvider.EnsureMemoryStoreCreatedAsync(chatModelDeployment, embeddingModelDeployment);
//Console.WriteLine("--- Session 1: Storing Memory ---");
//AgentSession session1 = await agent.CreateSessionAsync();

//AgentResponse response1 = await agent.RunAsync("Hi, I'm Aditya and I strictly eat gluten-free food.", session1);
//Console.WriteLine($"Agent: {response1.Text}");

//// Wait for asynchronous memory extraction processing on the service backend
//await memoryProvider.WhenUpdatesCompletedAsync();

//// 6. Simulate Session 2: Fresh session, testing long-term recall
//Console.WriteLine("\n--- Session 2: Recalling Memory ---");
//AgentSession session2 = await agent.CreateSessionAsync();

//AgentResponse response2 = await agent.RunAsync("What should we make for dinner tonight?", session2);
//Console.WriteLine($"Agent: {response2.Text}");






