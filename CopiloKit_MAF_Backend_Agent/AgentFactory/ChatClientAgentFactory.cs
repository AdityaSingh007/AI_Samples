using Azure.AI.OpenAI;
using CopiloKit_MAF_Backend_Agent.AgentTools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CopiloKit_MAF_Backend_Agent.AgentFactory
{
    internal static class ChatClientAgentFactory
    {
        public static AIAgent CreateHumanInTheLoop(string agentName, string modelName, AzureOpenAIClient client, AgentCustomTools tripPlannerTool)
        {
            AIAgent agent = client
                           .GetChatClient(modelName)
                           .AsIChatClient()
                           .AsAIAgent(
                                       new ChatClientAgentOptions()
                                       {
                                           Name = agentName,
                                           ChatOptions = new()
                                           {
                                               Instructions = "You are a trip planner agent which plans the trip as per days provided by the user with human-in-the-loop approval. " +
                                                  "When the user asks for a trip plan, prepare the itinerary and call the PlanTripWithApprovalRequest tool so the frontend can ask the user to confirm or reject the proposed plan. " +
                                                  "Do not state that the trip is finalized unless the user explicitly approves it in the client experience. " +
                                                  "Once the user confirms the trip show trip itineary in bulleted points daywise. ",
                                               Tools =
                                                 [
                                                      AIFunctionFactory.Create(tripPlannerTool.PlanTripWithApprovalRequest)
                                                 ]
                                           },
                                       }
                                       );

            return agent;
        }
    }
}
