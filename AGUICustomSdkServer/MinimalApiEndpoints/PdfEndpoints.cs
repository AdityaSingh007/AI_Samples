using AGUICustomSdkServer.PdfIngestion;
using AGUICustomSdkServer.SearchServices;
using Microsoft.AspNetCore.Mvc;

namespace AGUICustomSdkServer.MinimalApiEndpoints
{
    public static class PdfEndpoints
    {
        public static IEndpointRouteBuilder MapPdfEndpoints(this IEndpointRouteBuilder app)
        {
            var pdf = app.MapGroup("/api/pdf").WithTags("PDF");

            pdf.MapPost("/ingest", async ([FromServices] IPdfIngestion pdfIngestion) =>
            {
                var isIngested = await pdfIngestion.IngestPdfAsync();
                return isIngested
                    ? Results.Ok(new { message = "PDF ingestion completed." })
                    : Results.Problem("PDF ingestion failed.");
            }).WithName("IngestPdf");

            pdf.MapGet("/search", async (string query, int? topK, [FromServices] IPdfVectorSearch pdfVectorSearch) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest(new { message = "Query is required." });
                }

                var results = await pdfVectorSearch.SearchAsync(query, topK ?? 5);
                return Results.Ok(results);
            }).WithName("SearchPdf");

            return app;
        }
    }
}
