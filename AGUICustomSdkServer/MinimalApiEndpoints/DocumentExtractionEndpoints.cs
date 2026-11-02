using AGUICustomSdkServer.Interfaces;
using AGUICustomSdkServer.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace AGUICustomSdkServer.MinimalApiEndpoints
{
    public static class DocumentExtractionEndpoints
    {
        public static IEndpointRouteBuilder MapDocumentExtractionEndpoints(this IEndpointRouteBuilder app)
        {
            var documents = app.MapGroup("/api/documents").WithTags("Documents");

            documents.MapGet("/antiforgery", (HttpContext context, [FromServices] IAntiforgery antiforgery) =>
            {
                var tokens = antiforgery.GetAndStoreTokens(context);
                return Results.Ok(new { token = tokens.RequestToken });
            }).WithName("GetDocumentAntiforgeryToken");

            documents.MapPost("/extract", async ([FromForm] IFormFile file, [FromServices] IDocumentExtraction documentExtraction, [FromKeyedServices("se-qwen-chatClient")] IChatClient chatClient, CancellationToken cancellationToken) =>
            {
                if (file.Length == 0)
                {
                    return Results.BadRequest(new { message = "A non-empty file is required." });
                }

                var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return Results.BadRequest(new { message = "A valid file name is required." });
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (extension is not (".pdf" or ".docx" or ".xlsx" or ".csv"))
                {
                    return Results.BadRequest(new { message = "Supported file types are PDF, DOCX, XLSX and CSV." });
                }

                var uploadedFilePath = Path.Combine("Data", fileName);
                var outputFilePath = Path.Combine("Data", $"{Path.GetFileNameWithoutExtension(fileName)}_tree.json");
                try
                {
                    Directory.CreateDirectory("Data");
                    if (File.Exists(uploadedFilePath) || File.Exists(outputFilePath))
                    {
                        return Results.Conflict(new { message = "An uploaded file or JSON tree with this name already exists." });
                    }

                    await using (var stream = new FileStream(uploadedFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }

                    var text = await documentExtraction.ExtractText(uploadedFilePath);

                    string extractionPrompt = $$"""
            You are an advanced, strict architectural document parser. 
            Analyze the following document text content and build a hierarchical, high-level structural map index (JSON tree representation).
            Identify logical chapters, sheets, tables, or topics. Ensure the 'referenceLocation' matches structural elements like page numbers, row IDs, or headers found in the text.

            Document Extracted Context:
            {{text}}

            Respond ONLY with a valid JSON object matching this schema template:
            {
              "documentTitle": "Clean title describing document contents",
              "sections": [
                 { "id": "sec_1", "title": "Logical Section Name", "summary": "Granular summary of what this segment contains", "referenceLocation": "Page 3 / Sheet 1 / Row 5" }
              ]
            }
            """;

                    var stringBuilder = new StringBuilder();
                    await foreach (var update in chatClient.GetStreamingResponseAsync(extractionPrompt, new ChatOptions()
                    {
                        ResponseFormat = ChatResponseFormat.ForJsonSchema<DocumentStructureTree>(),
                    }))
                    {
                        if (update.Text is not null && !string.IsNullOrWhiteSpace(update.Text))
                        {
                            stringBuilder.Append(update.Text);
                            Console.Write(update.Text); // Progressively shows incoming JSON fragments
                        }
                    }
                    var jsonResponse = JsonSerializer.Deserialize<DocumentStructureTree>(stringBuilder.ToString());
                    // write object into a json file in data folder
                    await using (var outputStream = new FileStream(outputFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await JsonSerializer.SerializeAsync(outputStream, jsonResponse, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
                    }

                    return Results.Ok(new { message = "Ingestion completed." , filePath = outputFilePath, uploadedFilePath });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error processing document: {ex.Message}");
                }
            }).WithName("ExtractDocument")
              .DisableAntiforgery();

            return app;
        }
    }
}
