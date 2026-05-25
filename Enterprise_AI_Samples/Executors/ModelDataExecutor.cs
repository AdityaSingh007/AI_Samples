using CAE_AI_Samples.Models;
using Microsoft.Agents.AI.Workflows;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CAE_AI_Samples.Executors
{
    internal sealed partial class ModelDataExecutor(HttpClient httpClient) : Executor<Dictionary<string, ProjectDetail>, Dictionary<string, IEnumerable<ModelRootObject>>>("ModelDataExecutor")
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [MessageHandler]
        public override async ValueTask<Dictionary<string, IEnumerable<ModelRootObject>>> HandleAsync(Dictionary<string, ProjectDetail> message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var token = await context.ReadStateAsync<string>(
                                      key: "AuthToken",
                                      scopeName: "Security",
                                      cancellationToken: cancellationToken) ?? throw new ArgumentNullException("auth token is empty or null");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var outputForPipeline = new Dictionary<string, IEnumerable<ModelRootObject>>();
            if (message.TryGetValue("sourceProject", out var projectDetailSource) && message.TryGetValue("targetProject", out var projectDetailTarget))
            {
                var sourceModels = await GetModelsAsync("ModelManagement", projectDetailSource.ProjectId, httpClient);
                var targetModels = await GetModelsAsync("ModelManagement", projectDetailTarget.ProjectId, httpClient);
                outputForPipeline.Add("sourceModels", sourceModels);
                outputForPipeline.Add("targetModels", targetModels);
            }

            return outputForPipeline;
        }

        private async Task<IEnumerable<ModelRootObject>> GetModelsAsync(string url, int projectId, HttpClient httpClient)
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{url}/{projectId}/models");
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var models = JsonSerializer.Deserialize<ModelList>(responseJson, DeserializeOptions);

            if (models?.Models == null)
            {
                throw new ArgumentNullException(nameof(models));
            }

            var semaphore = new SemaphoreSlim(10);
            var modelDetailsConcurrentCollection = new ConcurrentBag<ModelRootObject>();

            var modelTasks = models.Models
            .Select(model => FetchModelDetailsAsync(model, projectId, httpClient, semaphore, modelDetailsConcurrentCollection))
            .ToList();

            await Task.WhenAll(modelTasks);

            ConsoleUi.WriteSuccess("All model details fetched.");

            return modelDetailsConcurrentCollection.ToList();
        }

        private async Task FetchModelDetailsAsync(ModelDetail model, int projectId, HttpClient httpClient, SemaphoreSlim semaphore, ConcurrentBag<ModelRootObject> modelRootObjects)
        {
            await semaphore.WaitAsync();
            try
            {
                var response = await httpClient.GetAsync($"ModelManagement/{projectId}/export-model/{model.ModelId}");
                response.EnsureSuccessStatusCode();
                var responseStream = await response.Content.ReadAsStreamAsync();
                var modelDetails = JsonSerializer.Deserialize<ModelRootObject>(responseStream, DeserializeOptions);
                if (modelDetails != null)
                {
                    modelRootObjects.Add(modelDetails);
                }
            }
            catch (Exception ex)
            {
                ConsoleUi.WriteError($"Error fetching details for model {model.Name}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
