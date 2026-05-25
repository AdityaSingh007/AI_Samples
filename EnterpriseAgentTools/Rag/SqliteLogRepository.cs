using CAEAgentTools.Data;
using CAEAgentTools.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CAEAgentTools.Rag
{
    public sealed class SqliteLogRepository(string databasePath) : ILogRepository
    {
        private readonly string databasePath = databasePath;

        public async Task<IReadOnlyList<TraceLogEntry>> GetLogsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            ValidateDateRange(fromDate, toDate);
            EnsureDatabaseExists();

            await using var dbContext = new TraceLogDbContext(databasePath);

            return await dbContext.TraceLogs
                .AsNoTracking()
                .Where(log => log.Timestamp >= fromDate && log.Timestamp <= toDate)
                .Where(log => log.Area != null)
                .OrderBy(log => log.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TraceLogEntry>> GetAllLogsAsync(CancellationToken cancellationToken = default)
        {
            EnsureDatabaseExists();

            await using var dbContext = new TraceLogDbContext(databasePath);

            return await dbContext.TraceLogs
                .AsNoTracking()
                .Where(log => log.Area != null)
                .OrderBy(log => log.Timestamp)
                .ToListAsync(cancellationToken);
        }

        private void EnsureDatabaseExists()
        {
            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException($"No SQLite database found at '{databasePath}'.", databasePath);
            }
        }

        private static void ValidateDateRange(DateTime fromDate, DateTime toDate)
        {
            if (toDate < fromDate)
            {
                throw new ArgumentException("The end date must be greater than or equal to the start date.", nameof(toDate));
            }
        }
    }
}
