namespace CAEAgentTools.Rag
{
    public interface ILogIngestionService
    {
        Task IngestAsync(CancellationToken cancellationToken = default);
    }
}
