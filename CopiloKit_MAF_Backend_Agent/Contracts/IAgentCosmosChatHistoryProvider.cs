using Microsoft.Agents.AI;

namespace CopiloKit_MAF_Backend_Agent.Contracts
{
    internal interface IAgentCosmosChatHistoryProvider
    {
        Task<CosmosChatHistoryProvider> GetCosmosChatHistoryProviderAsync();
    }
}
