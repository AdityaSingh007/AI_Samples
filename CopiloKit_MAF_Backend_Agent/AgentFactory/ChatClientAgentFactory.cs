using Azure.AI.OpenAI;
using CopiloKit_MAF_Backend_Agent.AgentTools;
using CopiloKit_MAF_Backend_Agent.AgentUtils;
using CopiloKit_MAF_Backend_Agent.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CopiloKit_MAF_Backend_Agent.AgentFactory
{
    internal static class ChatClientAgentFactory
    {
        public static AIAgent CreateHumanInTheLoopWithMiddleware(string agentName, string modelName,
            AzureOpenAIClient client, AgentCustomTools agentCustomTools)
        {
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
                                       }
                                       );

            var agent = baseAgent
                .AsBuilder()
                .Use(AuditAgentFunctionCalls)
                .Build();

            return agent;
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
