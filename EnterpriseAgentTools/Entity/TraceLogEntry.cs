using System.ComponentModel.DataAnnotations.Schema;

namespace CAEAgentTools.Entity
{
    [Table("Log")]
    public sealed class TraceLogEntry
    {
        public DateTime Timestamp { get; set; }

        public string LogLevel { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? Area { get; set; } = string.Empty;

        public string CallSite { get; set; } = string.Empty;

        public bool IsFunctional { get; set; }

        public bool IsApplicative { get; set; }
    }
}
