using AGUICustomSdkServer.Models;

namespace AGUICustomSdkServer.SearchServices
{
    public interface IPdfVectorSearch
    {
        Task<List<PdfChunk>> SearchAsync(string query, int topK = 5);
    }
}
