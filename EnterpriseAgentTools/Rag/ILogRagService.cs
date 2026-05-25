namespace CAEAgentTools.Rag
{
    public interface ILogRagService
    {
        Task<LogRagContext> RetrieveContextAsync(DateTime fromDate, DateTime toDate, string query, int maxResults = 10, CancellationToken cancellationToken = default);
    }
}
