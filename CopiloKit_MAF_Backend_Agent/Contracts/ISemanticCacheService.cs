namespace CopiloKit_MAF_Backend_Agent.Contracts
{
    public interface ISemanticCacheService
    {
        Task<string?> GetCachedResponseAsync(string userPrompt);
        Task CacheResponseAsync(string userPrompt, string completion);
    }
}
