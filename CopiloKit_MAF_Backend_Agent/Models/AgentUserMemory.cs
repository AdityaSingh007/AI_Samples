namespace CopiloKit_MAF_Backend_Agent.Models
{
    public class AgentUserMemory
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Scope { get; set; }
        public string Content { get; set; }
        public string[] SourceThreadIds { get; set; }
        public DateTime InvalidatedAt { get; set; }
    }
}
