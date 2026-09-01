namespace CopiloKit_MAF_Backend_Agent.Models
{
    public sealed record CreateMemoryRequest(
     string Content,
     string Kind,
     string Scope,
     string[] SourceThreadIds);
}
