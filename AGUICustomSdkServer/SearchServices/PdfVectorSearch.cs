using AGUICustomSdkServer.Models;
using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;

namespace AGUICustomSdkServer.SearchServices
{
    public class PdfVectorSearch : IPdfVectorSearch
    {
        private readonly IConfiguration _configuration;
        private readonly IEmbeddingGenerator _embeddingGenerator;
        private readonly string _vectorDbPath;
        public PdfVectorSearch(IConfiguration configuration, IEmbeddingGenerator embeddingGenerator)
        {
            this._configuration = configuration;
            this._vectorDbPath = configuration["QwenPdfVectorDbPath"] ?? throw new InvalidOperationException("QwenPdfVectorDbPath is not configured.");
            this._embeddingGenerator = embeddingGenerator;
        }
        public async Task<List<PdfChunk>> SearchAsync(string query, int topK = 5)
        {
            using var vectorStore = new SqliteVectorStore(_vectorDbPath, new SqliteVectorStoreOptions()
            {
                EmbeddingGenerator = _embeddingGenerator
            });
            var collection = vectorStore.GetCollection<string, PdfChunk>("pdf_chunks");

            var results = new List<PdfChunk>();
            await foreach (var r in collection.SearchAsync(query, topK))
            {
                results.Add(r.Record);
            }

            return results;
        }
    }
}
