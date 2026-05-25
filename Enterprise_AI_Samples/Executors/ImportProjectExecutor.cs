using CAE_AI_Samples.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CAE_AI_Samples.Executors
{
    internal sealed partial class ImportProjectExecutor(HttpClient httpClient, IMemoryCache memoryCache) : Executor<string, Dictionary<string, ProjectDetail>>("ImportProjectExecutor")
    {
        private readonly IMemoryCache memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        private readonly string ProjectImportEndpoint = "ProjectManagement/import-project";
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        [MessageHandler]
        public override async ValueTask<Dictionary<string, ProjectDetail>> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var token = await context.ReadStateAsync<string>(
                                      key: "AuthToken",
                                      scopeName: "Security",
                                      cancellationToken: cancellationToken) ?? throw new ArgumentNullException("auth token is empty or null");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string sourceProjectFile;
            string targetProjectFile;

            try
            {
                sourceProjectFile = Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cae_import_project", "source"))
                    .Single();
                targetProjectFile = Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cae_import_project", "target"))
                    .Single();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to locate project files. Please ensure that the source and target project files are placed in the correct directories.", ex);
            }

            ConsoleUi.WriteBlankLine();
            var sourceProjectImportPassword = ConsoleUi.PromptRequired("Source project import password", secret: true);
            var targetProjectImportPassword = ConsoleUi.PromptRequired("Target project import password", secret: true);

            using var sourceResponse = await PostImportProjectAsync(
                httpClient,
                sourceProjectFile,
                sourceProjectImportPassword,
                cancellationToken);

            sourceResponse.EnsureSuccessStatusCode();
            var sourceProject = await ReadImportResponseAsync(sourceResponse, cancellationToken);
            ConsoleUi.WriteSuccess($"Reference project imported successfully: {sourceProject.Project?.Name}");

            using var targetResponse = await PostImportProjectAsync(
                httpClient,
                targetProjectFile,
                targetProjectImportPassword,
                cancellationToken);
            targetResponse.EnsureSuccessStatusCode();
            var targetProject = await ReadImportResponseAsync(targetResponse, cancellationToken);
            ConsoleUi.WriteSuccess($"Comparison project imported successfully: {targetProject.Project?.Name}");

            memoryCache.Set("SourceProjectId", sourceProject.Project?.ProjectId ?? 0);
            memoryCache.Set("TargetProjectId", targetProject.Project?.ProjectId ?? 0);

            var projectImportDictionary = new Dictionary<string, ProjectDetail>();
            projectImportDictionary.Add("sourceProject", new ProjectDetail { ProjectId = sourceProject.Project?.ProjectId ?? 0, ProjectName = sourceProject.Project?.Name ?? string.Empty });
            projectImportDictionary.Add("targetProject", new ProjectDetail { ProjectId = targetProject.Project?.ProjectId ?? 0, ProjectName = targetProject.Project?.Name ?? string.Empty });

            ConsoleUi.WriteHighlight($"Comparison of model shall be done between {sourceProject.Project?.Name} => {targetProject.Project?.Name}");
            return projectImportDictionary;
        }

        private async Task<HttpResponseMessage> PostImportProjectAsync(HttpClient httpClient, string projectPath, string password, CancellationToken cancellationToken)
        {
            var projectBytes = await File.ReadAllBytesAsync(projectPath, cancellationToken);

            using var formData = new MultipartFormDataContent();
            using var projectContent = new ByteArrayContent(projectBytes);
            projectContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            formData.Add(projectContent, "File", Path.GetFileName(projectPath));
            formData.Add(new StringContent(password), "password");
            formData.Add(new StringContent(bool.FalseString.ToLowerInvariant()), "upgradeModelDefinition");

            return await httpClient.PostAsync(ProjectImportEndpoint, formData, cancellationToken);
        }

        private static async Task<ProjectImportResponse> ReadImportResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var importResponse = await JsonSerializer.DeserializeAsync<ProjectImportResponse>(responseStream, jsonSerializerOptions, cancellationToken);

            if (importResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize project import response.");
            }

            return importResponse;
        }
    }
}
