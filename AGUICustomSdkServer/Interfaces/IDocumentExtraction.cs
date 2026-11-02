namespace AGUICustomSdkServer.Interfaces
{
    public interface IDocumentExtraction
    {
        Task<string> ExtractText(string filePath);
    }
}
