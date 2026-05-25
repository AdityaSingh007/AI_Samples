using CAEAgentTools.Entity;
using CAEAgentTools.VectorStore;

namespace CAEAgentTools.Rag
{
    public interface ILogVectorStore
    {
        Task IngestAsync(IEnumerable<TraceLogEntry> traceLogs, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LlmVectorSearchResult>> SearchAsync(string query, DateTime fromDate, DateTime toDate, int maxResults = 10, CancellationToken cancellationToken = default);
    }
}
