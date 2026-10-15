using AGUICustomSdkServer.SearchServices;
using System.ComponentModel;
using System.Text.Json;

namespace AGUICustomSdkServer.AgentTools
{
    public class AgentTool(IPdfVectorSearch pdfVectorSearch)
    {
        private readonly IPdfVectorSearch _pdfVectorSearch = pdfVectorSearch;

        [Description("Searches Qwen reference content from the ingested PDF vector store and returns relevant chunks as JSON.")]
        public async Task<string> SearchQwenKnowledge(string query, int topK = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return JsonSerializer.Serialize(new { error = "Query is required." });
            }

            try
            {
                var results = await _pdfVectorSearch.SearchAsync(query, topK);
                return JsonSerializer.Serialize(results, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        [Description("Searches Car Lease reference content from the ingested PDF vector store and returns relevant chunks as JSON.")]
        public async Task<string> SearchCarLeaseKnowledge(string query, int topK = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return JsonSerializer.Serialize(new { error = "Query is required." });
            }

            try
            {
                var results = await _pdfVectorSearch.SearchAsync(query, topK);
                return JsonSerializer.Serialize(results, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }
    }
}
