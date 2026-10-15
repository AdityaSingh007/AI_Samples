using Microsoft.Extensions.VectorData;

namespace AGUICustomSdkServer.Models
{
    public class PdfChunk
    {
        [VectorStoreKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [VectorStoreData]
        public string DocumentName { get; set; } = string.Empty;
        [VectorStoreData]
        public int PageNumber { get; set; }
        [VectorStoreData]
        public string TextContent { get; set; } = string.Empty;

        // The vector is automatically generated from Text when an
        // IEmbeddingGenerator is configured on the collection or vector store
        [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineDistance)]
        public string Embedding => this.TextContent;

    }
}
