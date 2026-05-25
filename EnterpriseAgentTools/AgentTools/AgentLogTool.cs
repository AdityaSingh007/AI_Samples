using CAEAgentTools.Rag;
using System.ComponentModel;
using System.Text.Json;

namespace CAEAgentTools.AgentTools
{
    public class AgentLogTool(ILogRepository logRepository, ILogRagService logRagService)
    {
        private readonly ILogRepository logRepository = logRepository;
        private readonly ILogRagService logRagService = logRagService;

        [Description("Retrieves grounded log context for the specified date range using the startup-ingested vector store and returns agent-ready RAG context as JSON.")]
        public async Task<string> SearchLogs(DateTime fromDate, DateTime toDate, string query, int maxResults = 10)
        {
            try
            {
                var context = await logRagService.RetrieveContextAsync(fromDate, toDate, query, maxResults);

                return JsonSerializer.Serialize(context, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while searching logs: {ex.Message}");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }
    }
}
