using Microsoft.Extensions.VectorData;

namespace Enterprise_MAF_Samples.Models
{
    public class ScopedMemoryItem
    {
        [VectorStoreKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // "Global", "User:123", or "Session:xyz"
        [VectorStoreData]
        public string Scope { get; set; } = string.Empty;

        [VectorStoreData]
        public string FactText { get; set; } = string.Empty;

        // Vector size maps directly to the embedding model (e.g., 1536 for text-embedding-3-small)
        [VectorStoreVector(dimensions: 1536, DistanceFunction = DistanceFunction.CosineDistance)]
        public string Embedding => this.FactText;
    }
}
