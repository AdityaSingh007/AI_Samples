using CAEAgentTools.Entity;

namespace CAEAgentTools.Rag
{
    public interface ILogRepository
    {
        Task<IReadOnlyList<TraceLogEntry>> GetLogsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TraceLogEntry>> GetAllLogsAsync(CancellationToken cancellationToken = default);
    }
}
