using CAEAgentTools.Entity;
using Microsoft.EntityFrameworkCore;

namespace CAEAgentTools.Data
{
    public sealed class TraceLogDbContext(string databasePath) : DbContext
    {
        public DbSet<TraceLogEntry> TraceLogs => Set<TraceLogEntry>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TraceLogEntry>(entity =>
            {
                entity.Property(log => log.Timestamp).HasColumnName("Timestamp");
                entity.Property(log => log.LogLevel).HasColumnName("LogLevel");
                entity.Property(log => log.Message).HasColumnName("Message");
                entity.Property(log => log.Area).HasColumnName("Area");
                entity.Property(log => log.CallSite).HasColumnName("CallSite");
                entity.Property(log => log.IsFunctional).HasColumnName("IsFunctional");
                entity.Property(log => log.IsApplicative).HasColumnName("IsApplicative");
                entity.HasNoKey();
            });
        }
    }
}
