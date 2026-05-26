using Azure.AI.OpenAI;
using CAEAgentTools.Entity;
using CAEAgentTools.Rag;
using CAEAgentTools.VectorStore;
using EnterpriseAgentTools.VectorMetadata;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqliteVec;

namespace EnterpriseAgentTools.VectorStore
{
    public sealed class SqliteVectorStoreProvider : ILogVectorStore
    {
        private string azureOpenAIApiKey = Environment.GetEnvironmentVariable("Open_AI_Embedding_Model_Token", EnvironmentVariableTarget.User) ?? throw new ArgumentNullException($"Api key {nameof(azureOpenAIApiKey)} is empty");
        private IEmbeddingGenerator embeddingGenerator;
        private SqliteVectorStore sqliteVectorStore;
        private SqliteCollection<string, LogVectorRecord> logCollection;
        public SqliteVectorStoreProvider()
        {
            embeddingGenerator = new AzureOpenAIClient(new Uri("https://singhadi041-6488-resource.services.ai.azure.com"),
               new System.ClientModel.ApiKeyCredential(azureOpenAIApiKey))
                                    .GetEmbeddingClient("text-embedding-3-small")
                                    .AsIEmbeddingGenerator();
            sqliteVectorStore = new SqliteVectorStore("Datasource=./Data/logVector.db", new() { EmbeddingGenerator = embeddingGenerator });
            logCollection = sqliteVectorStore.GetCollection<string, LogVectorRecord>("logRecords");
            logCollection.EnsureCollectionExistsAsync().Wait();
        }

        public async Task IngestAsync(IEnumerable<TraceLogEntry> traceLogs, CancellationToken cancellationToken = default)
        {
            var logs = traceLogs as IList<TraceLogEntry> ?? traceLogs.ToList();

            Console.WriteLine($"Ingesting {logs.Count} log entries into the vector store...");

            cancellationToken.ThrowIfCancellationRequested();

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            };

            await Parallel.ForEachAsync(logs, parallelOptions, async (log, ct) =>
            {
                await logCollection.UpsertAsync(new LogVectorRecord
                {
                    Message = log.Message,
                    Area = log.Area ?? string.Empty,
                    LogTimestamp = log.Timestamp,
                    LogLevel = log.LogLevel,
                }, ct);
            });

            Console.WriteLine("Ingestion completed.");
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

            var results = new List<LlmVectorSearchResult>(maxResults);

            var searchOptions = new VectorSearchOptions<LogVectorRecord>
            {
                Filter = record => record.LogTimestamp >= fromDate && record.LogTimestamp <= toDate,
            };

            await foreach (var result in logCollection.SearchAsync(query, top: maxResults, options: searchOptions, cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var record = result.Record;

                results.Add(new LlmVectorSearchResult
                {
                    Rank = result.Score,
                    Query = query,
                    Area = record.Area,
                    Timestamp = record.LogTimestamp,
                    Content = record.Message,
                    LogLevel = record.LogLevel
                });

                if (results.Count == maxResults)
                {
                    break;
                }
            }

            return results;
        }
    }
}
