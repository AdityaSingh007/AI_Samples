using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Memory;
using Azure.Identity;
using CopiloKit_MAF_Backend_Agent.AgentTools;
using CopiloKit_MAF_Backend_Agent.AgentUtils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Text.Json;

namespace CopiloKit_MAF_Backend_Agent.AgentFactory
{
    internal static class ChatClientAgentFactory
    {
        private const string memoryStoreName = "customer-long-term-vault";
        private const string deploymentName = "gpt-4.1-mini";
        private const string embeddingModel = "text-embedding-3-small";
        private const string chatModelDeployment = "gpt-4.1-mini";

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
    }
}
