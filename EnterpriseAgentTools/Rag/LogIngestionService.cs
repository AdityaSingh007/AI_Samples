namespace CAEAgentTools.Rag
{
    public sealed class LogIngestionService(ILogRepository logRepository, ILogVectorStore logVectorStore) : ILogIngestionService
    {
        private readonly ILogRepository logRepository = logRepository;
        private readonly ILogVectorStore logVectorStore = logVectorStore;

        public async Task IngestAsync(CancellationToken cancellationToken = default)
        {
            var logs = await logRepository.GetAllLogsAsync(cancellationToken);
            await logVectorStore.IngestAsync(logs, cancellationToken);
        }
    }
}
