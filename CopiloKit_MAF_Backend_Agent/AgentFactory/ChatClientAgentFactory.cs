using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Memory;
using Azure.Identity;
using CopiloKit_MAF_Backend_Agent.AgentTools;
using CopiloKit_MAF_Backend_Agent.AgentUtils;
using CopiloKit_MAF_Backend_Agent.Contracts;
using CopiloKit_MAF_Backend_Agent.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Agents.AI.Foundry;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CopiloKit_MAF_Backend_Agent.AgentFactory
{
    internal static class ChatClientAgentFactory
    {
        #region Constants
        private const string memoryStoreName = "customer-long-term-vault";
        private const string deploymentName = "gpt-4.1-mini";
        private const string embeddingModel = "text-embedding-3-small";
        private const string chatModelDeployment = "gpt-4.1-mini";
        #endregion

        #region Agents
        public static async Task<AIAgent> CreateAgentWithMemory(
            string agentName,
            string modelName,
            AzureOpenAIClient client,
            IAgentCosmosChatHistoryProvider agentCosmosChatHistoryProvider,
            ISemanticCacheService semanticCacheService
           )
        {
            var cosmosChatHistoryProvider = await agentCosmosChatHistoryProvider.GetCosmosChatHistoryProviderAsync();
#pragma warning disable MAAI001
            var compactionStrategy = new SummarizationCompactionStrategy(
                chatClient: client.GetChatClient(modelName).AsIChatClient(),
                trigger: CompactionTriggers.MessagesExceed(8)
                );
            var compactionProvider = new CompactionProvider(compactionStrategy);
#pragma warning restore MAAI001
            var baseAgent = client
                           .GetChatClient(modelName)
                           .AsIChatClient()
                           .AsAIAgent(
                                       new ChatClientAgentOptions
                                       {
                                           Name = "PersonalAssistant",
                                           ChatOptions = new()
                                           {
                                               ModelId = "gpt-4.1-mini",
                                               Instructions = @"You are an personal assistant. Always address the user by his/her name.",
                                               //Tools = [AIFunctionFactory.Create(AgentCustomTools.RecallMemory, options: new() { Name = "recall_memory" })]
                                           },
                                           ChatHistoryProvider = cosmosChatHistoryProvider,
                                           AIContextProviders = [compactionProvider],

                                       }
                                       );
#pragma warning restore MAAI001

            var agent = baseAgent
                .AsBuilder()
                .Use(
                    runFunc: (messages, session, options, innerAgent, cancellationToken) =>
                        CustomAgentRunStreamingMiddleware(messages, session, options,
                        innerAgent, client, semanticCacheService, cancellationToken),
                    runStreamingFunc: null)
                .Build();

            return agent;
        }

        public static async Task<AIAgent> CreateHumanInTheLoopWithMiddleware(
            string agentName,
            string modelName,
            AzureOpenAIClient client,
            AgentCustomTools agentCustomTools,
            string foundryProjectEndpoint = "",
            string apiKey = "",
            string tenantId = "",
            string clientId = "",
            string clientSecret = ""
           )
        {
            try
            {
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var projectClient = new AIProjectClient
                                    (
                                    new Uri(foundryProjectEndpoint),
                                    tokenProvider: credential
                                    );
                MemoryStoreDefaultDefinition memoryStoreDefinition = new(
                                             chatModel: chatModelDeployment,
                                             embeddingModel: embeddingModel
                                             );
                memoryStoreDefinition.Options = new(isUserProfileEnabled: true, isChatSummaryEnabled: true);
                //memoryStoreDefinition.Options.UserProfileDetails = "store user preferences";
                try
                {
                    projectClient.MemoryStores.CreateMemoryStore(
                        name: memoryStoreName,
                        definition: memoryStoreDefinition,
                        description: "Memory store for customer support agent");
                }
                catch (ClientResultException ex) when (ex.Status == 400)
                {
                    Console.WriteLine(ex.Message);
                }

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var baseAgent = client
                           .GetChatClient(modelName)
                           .AsIChatClient()
                           .AsAIAgent(
                                       new ChatClientAgentOptions()
                                       {
                                           Name = agentName,
                                           ChatOptions = new()
                                           {
                                               Instructions = AgentInstruction.HumanInTheLoopAgentInstruction,
                                               Tools =
                                                 [
                                                     AIFunctionFactory.Create(agentCustomTools.GenerateRandomUserName),
                                                     AIFunctionFactory.Create(agentCustomTools.SendEmail),
                                                 ]
                                           },
                                           AIContextProviders =
                                           [
                                               new FoundryMemoryProvider(
                                                   projectClient,
                                                   memoryStoreName,
                                                   stateInitializer: _ => new(
                                                       new FoundryMemoryProviderScope($"user001")))
                                           ]
                                       }
                                       );
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                var agent = baseAgent
                    .AsBuilder()
                    .Use(AuditAgentFunctionCalls)
                    .Build();

                return agent;
            }
            finally
            {

            }


        }

        public static async Task<AIAgent> CreateResposneAnanlysisAgent(
            AzureOpenAIClient client,
            AgentCustomTools? agentCustomTools
           )
        {
            var baseAgent = client
                           .GetChatClient(chatModelDeployment)
                           .AsIChatClient()
                           .AsAIAgent(new ChatClientAgentOptions()
                           {
                               Name = "AgentResponseAnalysis",
                               ChatOptions = new()
                               {
                                   Instructions = AgentInstruction.AgentResponseAnalysisInstruction,
                                   ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<EvaluationResult>()
                               },
                           });

            return baseAgent;
        }
        #endregion

        #region Middlewares
        private static async Task<AgentResponse> CustomAgentRunStreamingMiddleware
        (
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        AzureOpenAIClient azureOpenAIClient,
        ISemanticCacheService semanticCacheService,
        CancellationToken cancellationToken
        )
        {
            Console.WriteLine($"Executing middleware:- {messages.Count()}");

            var userPrompt = messages
                .LastOrDefault(m => m.Role == Microsoft.Extensions.AI.ChatRole.User)
                ?.Text;

            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                var cachedResponse = await semanticCacheService.GetCachedResponseAsync(userPrompt);
                if (!string.IsNullOrWhiteSpace(cachedResponse))
                {
                    return new AgentResponse
                    {
                        Messages =
                        [
                            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, cachedResponse)
                        ]
                    };
                }
            }

            var llmResponse = await innerAgent.RunAsync(messages, session, options, cancellationToken);

            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                var assistantResponse = llmResponse.Messages
                    .Where(m => m.Role == Microsoft.Extensions.AI.ChatRole.Assistant)
                    .Select(m => m.Text!.Trim())
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(assistantResponse))
                {
                    var evaluationAgent = await CreateResposneAnanlysisAgent(azureOpenAIClient, null);
                    string evaluationPrompt = $"""
                           User Prompt: {userPrompt}
                           Agent Response to Evaluate: {assistantResponse}
                           """;
                    AgentResponse respone = await evaluationAgent.RunAsync<EvaluationResult>(evaluationPrompt);
                    var finalMessage = respone.Messages[^1];
                    var evaluationResult = System.Text.Json.JsonSerializer.Deserialize<EvaluationResult>(finalMessage.Text, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new EvaluationResult { IsAnswer = false, Reasoning = "Failed to parse evaluation response." };
                    if (evaluationResult.IsAnswer)
                    {
                        await semanticCacheService.CacheResponseAsync(userPrompt, assistantResponse);
                    }

                }
            }

            return llmResponse;
        }

        public static async ValueTask<object?> AuditAgentFunctionCalls(
            AIAgent agent,
            FunctionInvocationContext context,
            Func<FunctionInvocationContext, CancellationToken,
            ValueTask<object?>> next,
            CancellationToken cancellationToken)
        {
            var functionName = context.Function.Name;
            var args = string.Join(", ", context.Arguments.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var timestamp = DateTime.UtcNow;
            Console.WriteLine($"[Agent] [FunctionCall] [Audit] [{timestamp:HH:mm:ss}] Agent '{agent.Name}' invoking '{functionName}({args})'", ConsoleColor.Yellow);

            bool approvalAction = false;
            if (string.Equals(functionName, nameof(AgentCustomTools.GenerateRandomUserName), StringComparison.OrdinalIgnoreCase) &&
                (!TryGetBooleanArgument(context, "isConfirmed", out approvalAction) || !approvalAction))
            {
                //var approvalRequest = new FrontendApprovalRequest(
                //    ToolName: "ConfirmSensitiveAction",
                //    Arguments: new ConfirmActionInput(
                //        Message: $"Agent wants to invoke '{functionName}'. Do you want to allow this action?",
                //        IsConfirmed: false));

                Console.WriteLine($"[Agent] [FunctionCall] [Audit] [{timestamp:HH:mm:ss}] PENDING_APPROVAL | Confirmation required before invoking '{functionName}'.", ConsoleColor.Yellow);
                //return approvalRequest;
            }

            try
            {
                var result = await next(context, cancellationToken);
                Console.WriteLine($"[Agent] [FunctionCall] [Audit] [{timestamp:HH:mm:ss}] COMPLETED | Result: {result}", ConsoleColor.Green);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agent] [FunctionCall] [Audit] [{timestamp:HH:mm:ss}] FAILED | Error: {ex.Message}", ConsoleColor.Red);
                throw;
            }
        }
        #endregion

        #region Private Helper Methods
        private static bool TryGetBooleanArgument(FunctionInvocationContext context, string argumentName, out bool value)
        {
            if (context.Arguments.TryGetValue(argumentName, out var rawValue))
            {
                switch (rawValue)
                {
                    case bool boolValue:
                        value = boolValue;
                        return true;
                    case string stringValue when bool.TryParse(stringValue, out var parsed):
                        value = parsed;
                        return true;
                    case JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False } jsonValue:
                        value = jsonValue.GetBoolean();
                        return true;
                }
            }

            value = false;
            return false;
        }

        private static string GetStringArgument(FunctionInvocationContext context, string argumentName)
        {
            if (!context.Arguments.TryGetValue(argumentName, out var rawValue) || rawValue is null)
            {
                return string.Empty;
            }

            return rawValue switch
            {
                JsonElement { ValueKind: JsonValueKind.String } jsonValue => jsonValue.GetString() ?? string.Empty,
                _ => rawValue.ToString() ?? string.Empty
            };
        }

        #endregion
    }
}
