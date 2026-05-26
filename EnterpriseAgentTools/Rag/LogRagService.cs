using CAEAgentTools.VectorStore;

namespace CAEAgentTools.Rag
{
    public sealed class LogRagService(ILogVectorStore logVectorStore) : ILogRagService
    {
        private readonly ILogVectorStore logVectorStore = logVectorStore;

        public async Task<LogRagContext> RetrieveContextAsync(DateTime fromDate, DateTime toDate, string query, int maxResults = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("A search query is required.", nameof(query));
            }

            if (toDate < fromDate)
            {
                throw new ArgumentException("The end date must be greater than or equal to the start date.", nameof(toDate));
            }

            var candidateCount = Math.Max(maxResults * 5, maxResults);
            var retrievedResults = await logVectorStore.SearchAsync(query, fromDate, toDate, candidateCount, cancellationToken);

            Console.WriteLine($"Retrieved {retrievedResults.Count} candidate log entries from vector store for query: '{query}'");

            var matches = retrievedResults
                .Take(maxResults)
                .Select((result, index) => new LogRagContextItem
                {
                    Rank = index + 1,
                    Timestamp = result.Timestamp,
                    Area = result.Area,
                    LogLevel = result.LogLevel,
                    CallSite = result.CallSite,
                    Content = result.Content
                })
                .ToList();

            return new LogRagContext
            {
                Query = query,
                FromDate = fromDate,
                ToDate = toDate,
                RetrievedAtUtc = DateTime.UtcNow,
                Matches = matches
            };
        }
    }
}
