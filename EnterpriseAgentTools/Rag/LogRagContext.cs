namespace CAEAgentTools.Rag
{
    public sealed class LogRagContext
    {
        public string Query { get; init; } = string.Empty;

        public DateTime FromDate { get; init; }

        public DateTime ToDate { get; init; }

        public DateTime RetrievedAtUtc { get; init; }

        public string Instructions { get; init; } = "Use only the retrieved log entries as grounding context. If the logs do not contain enough evidence, say that the answer is inconclusive.";

        public IReadOnlyList<LogRagContextItem> Matches { get; init; } = [];
    }

    public sealed class LogRagContextItem
    {
        public int Rank { get; init; }

        public DateTime? Timestamp { get; init; }

        public string Area { get; init; } = string.Empty;

        public string LogLevel { get; init; } = string.Empty;

        public string CallSite { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;
    }
}
