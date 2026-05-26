using Microsoft.Extensions.VectorData;

namespace EnterpriseAgentTools.VectorMetadata
{
    public class LogVectorRecord
    {
        [VectorStoreKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [VectorStoreData]
        public string LogLevel { get; set; } = string.Empty;

        [VectorStoreData]
        public string Message { get; set; } = string.Empty;

        [VectorStoreData(IsIndexed = true)]
        public string Area { get; set; } = string.Empty;

        [VectorStoreData(IsIndexed = true)]
        public DateTime LogTimestamp { get; set; }

        // The vector is automatically generated from Text when an
        // IEmbeddingGenerator is configured on the collection or vector store
        [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineDistance)]
        public string Embedding => this.Message;

    }
}
