using CAE_AI_Samples.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CAE_AI_Samples.Executors
{
    internal sealed partial class TempJsonFileGeneratorExecutor(IMemoryCache memoryCache) : Executor<Dictionary<string, IEnumerable<ModelRootObject>>, string>("TempJsonFileGeneratorExecutor")
    {
        private readonly IMemoryCache memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        JsonSerializerOptions SerializeOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [MessageHandler]
        [YieldsOutput(typeof(string))]
        public override async ValueTask<string> HandleAsync(Dictionary<string, IEnumerable<ModelRootObject>> message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var output = new Dictionary<string, string>();
            foreach (var kvp in message)
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"{kvp.Key}.json");
                await File.WriteAllTextAsync(tempFilePath, System.Text.Json.JsonSerializer.Serialize(kvp.Value, SerializeOptions));
                output.Add((kvp.Key + "_FilePath").ToLower(), tempFilePath);
                memoryCache.Set((kvp.Key + "_FilePath").ToLower(), tempFilePath);
            }

            ConsoleUi.WriteSuccess("Temp JSON files generated and paths cached successfully.");

            return JsonSerializer.Serialize(output);
        }
    }
}
