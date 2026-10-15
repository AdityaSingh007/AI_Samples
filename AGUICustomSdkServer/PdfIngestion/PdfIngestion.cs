using AGUICustomSdkServer.Models;
using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using UglyToad.PdfPig;

namespace AGUICustomSdkServer.PdfIngestion
{
    public class PdfIngestion : IPdfIngestion
    {
        private readonly IConfiguration _configuration;
        private readonly string _vectorDbPath;
        private readonly IEmbeddingGenerator _embeddingGenerator;

        public PdfIngestion(IConfiguration configuration, IEmbeddingGenerator embeddingGenerator)
        {
            this._configuration = configuration;
            this._vectorDbPath = configuration["QwenPdfVectorDbPath"] ?? throw new InvalidOperationException("QwenPdfVectorDbPath is not configured.");
            this._embeddingGenerator = embeddingGenerator;
        }

        private async Task EnsureVectorDbDirectoryExists()
        {
            var directoryPath = "Data";
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
            Directory.CreateDirectory(directoryPath);

            // 2. Initialize the SQLite Vector Store
            using var vectorStore = new SqliteVectorStore(_vectorDbPath, new SqliteVectorStoreOptions()
            {
                EmbeddingGenerator = _embeddingGenerator
            });
            var collection = vectorStore.GetCollection<string, PdfChunk>("pdf_chunks");

            // Ensure the SQLite table (and underlying virtual vec_ table) exists
            await collection.EnsureCollectionExistsAsync();
        }

        public async Task<bool> IngestPdfAsync()
        {
            try
            {
                await EnsureVectorDbDirectoryExists();
                var pdfFilePath = _configuration["QwenPdfFilePath"] ?? throw new InvalidOperationException("QwenPdfFilePath is not configured.");
                using var pdfDocument = PdfDocument.Open(pdfFilePath);
                foreach (var page in pdfDocument.GetPages())
                {
                    var textContent = page.Text;
                    if (string.IsNullOrWhiteSpace(textContent)) continue;
                    var pdfChunk = new PdfChunk
                    {
                        DocumentName = Path.GetFileName(pdfFilePath),
                        PageNumber = page.Number,
                        TextContent = textContent
                    };
                    using var vectorStore = new SqliteVectorStore(_vectorDbPath, new SqliteVectorStoreOptions()
                    {
                        EmbeddingGenerator = _embeddingGenerator
                    });
                    var collection = vectorStore.GetCollection<string, PdfChunk>("pdf_chunks");
                    await collection.UpsertAsync(pdfChunk);
                }

                Console.WriteLine($"PDF ingestion completed for {Path.GetFileName(pdfFilePath)}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
