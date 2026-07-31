using Newtonsoft.Json;

namespace CopiloKit_MAF_Backend_Agent.Models
{
    public class SemanticCacheItem
    {
        [JsonProperty("id")]
        public string Id { get; } = Guid.NewGuid().ToString();
        [JsonProperty("pk")]
        public string Pk { get; set; } = "cache";
        public string Prompt { get; set; } = string.Empty;
        public string Completion { get; set; } = string.Empty;
        public float[] Embedding { get; set; }
    }
}
