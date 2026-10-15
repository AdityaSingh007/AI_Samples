namespace AGUICustomSdkServer.PdfIngestion
{
    public interface IPdfIngestion
    {
        Task<bool> IngestPdfAsync();
    }
}
