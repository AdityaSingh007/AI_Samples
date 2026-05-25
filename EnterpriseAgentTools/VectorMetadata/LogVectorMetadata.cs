namespace CAEAgentTools.VectorMetadata
{
    internal class LogVectorMetadata
    {
        public string Area { get; set; } = string.Empty;

        public string LogLevel { get; set; } = string.Empty;

        public string CallSite { get; set; } = string.Empty;

        public DateTime LogTimestamp { get; set; }
    }
}
