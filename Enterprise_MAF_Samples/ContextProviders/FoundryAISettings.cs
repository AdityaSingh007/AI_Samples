namespace Enterprise_MAF_Samples.ContextProviders
{
    public class FoundryAISettings
    {
        public string TenantId { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string FoundryProjectUrl { get; set; } = null!;
        public string Scope { get; set; } = null!;
        public string MemoryStoreName { get; set; } = null!;
        public string ModelToken { get; set; } = null!;
        public string ModelName { get; set; } = null!;
        public string EmbeddingModelName { get; set; } = null!;
        public string FoundryModelDeploymentUrl { get; set; } = null!;
    }
}
