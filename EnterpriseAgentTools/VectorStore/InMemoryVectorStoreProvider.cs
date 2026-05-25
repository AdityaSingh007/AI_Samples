using Build5Nines.SharpVector;
using CAEAgentTools.Entity;
using CAEAgentTools.Rag;
using CAEAgentTools.VectorMetadata;

namespace CAEAgentTools.VectorStore
{
    public sealed class InMemoryVectorStoreProvider : ILogVectorStore
    {
        private MemoryVectorDatabase<LogVectorMetadata> logVectorDatabase = new();

        public async Task IngestAsync(IEnumerable<TraceLogEntry> traceLogs, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logVectorDatabase = new MemoryVectorDatabase<LogVectorMetadata>();

            foreach (var log in traceLogs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await logVectorDatabase.AddTextAsync(
                    log.Message,
                    new LogVectorMetadata
                    {
                        Area = log.Area ?? string.Empty,
                        LogTimestamp = log.Timestamp,
                        LogLevel = log.LogLevel,
                        CallSite = log.CallSite,
                    });
            }
        }

        public async Task<IReadOnlyList<LlmVectorSearchResult>> SearchAsync(string query, DateTime fromDate, DateTime toDate, int maxResults = 10, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(query) || maxResults <= 0)
            {
                return [];
            }

            if (toDate < fromDate)
            {
                throw new ArgumentException("The end date must be greater than or equal to the start date.", nameof(toDate));
            }

            var results = await logVectorDatabase.SearchAsync(query);

            return results.Texts
                .Where(item => item.Metadata?.LogTimestamp >= fromDate && item.Metadata?.LogTimestamp <= toDate)
                .Take(maxResults)
                .Select((item, index) => new LlmVectorSearchResult
                {
                    Rank = index + 1,
                    Query = query,
                    Area = item.Metadata?.Area ?? string.Empty,
                    Timestamp = item.Metadata?.LogTimestamp,
                    Content = item.Text,
                    LogLevel = item.Metadata?.LogLevel ?? string.Empty,
                    CallSite = item.Metadata?.CallSite ?? string.Empty,
                })
                .ToList();
        }

        private static string BuildContent(TraceLogEntry log)
        {
            return string.Join(' ', new[]
            {
                log.Area,
                log.LogLevel,
                log.CallSite,
                log.Message
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }

    public sealed class LlmVectorSearchResult
    {
        public int Rank { get; init; }

        public string Query { get; init; } = string.Empty;

        public string Area { get; init; } = string.Empty;

        public DateTime? Timestamp { get; init; }

        public string Content { get; init; } = string.Empty;

        public string LogLevel { get; init; } = string.Empty;

        public string CallSite { get; init; } = string.Empty;
    }
}
