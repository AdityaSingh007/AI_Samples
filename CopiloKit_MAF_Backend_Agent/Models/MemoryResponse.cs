namespace CopiloKit_MAF_Backend_Agent.Models
{
    public sealed record MemoryResponse(
    string Id,
    string Kind,
    string Scope,
    string Content,
    string[] SourceThreadIds,
    DateTimeOffset? InvalidatedAt);
}
