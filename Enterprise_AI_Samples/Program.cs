using CAE_AI_Samples;
using CAE_AI_Samples.AITools;
using CAE_AI_Samples.Executors;
using CAE_AI_Samples.Models;
using GitHub.Copilot.SDK;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.GitHub.Copilot;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Text;
using System.Text.Json;

var services = new ServiceCollection();

services.AddMemoryCache(options =>
{
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(20);
});

services.AddHttpClient("cae_api_client", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/cae/v1/api/");
})
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    });

var serviceProvider = services.BuildServiceProvider();
var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

string apiHealthCheckUrl = "CaeSystem/health-check";

using var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("cae_api_client");

PrintStartupChecklist();

try
{
    var cliPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vendor", "copilot", "copilot.exe");

    var gitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN", EnvironmentVariableTarget.User)
               ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set. Please set it in your user environment variables and try again.");

    if (string.IsNullOrWhiteSpace(gitHubToken))
    {
        throw new InvalidOperationException("GITHUB_TOKEN environment variable is empty. Please set it in your system environment variables and try again.");
    }

    if (!File.Exists(cliPath))
    {
        throw new FileNotFoundException($"GitHub Copilot CLI exe not found at expected path: {cliPath}. Please ensure it is installed and try again.");
    }

    using var response = await httpClient.GetAsync(apiHealthCheckUrl);
    response.EnsureSuccessStatusCode();

    ConsoleUi.WriteSuccess("CAE web service is accessible.");
    ConsoleUi.WriteBlankLine();

    // Initialize the client
    await using CopilotClient copilotClient = new(new CopilotClientOptions()
    {
        CliPath = cliPath,
        GitHubToken = gitHubToken,
        UseLoggedInUser = false
    });

    await copilotClient.StartAsync();

    var gitHubCoPilotSession = new SessionConfig()
    {
        Model = "GPT-5.4",
        Streaming = true,
        OnPermissionRequest = PermissionHandler.ApproveAll,
        InfiniteSessions = new InfiniteSessionConfig { Enabled = true }
    };

    GitHubCopilotAgent caeModelAgent = new GitHubCopilotAgent(copilotClient,
        gitHubCoPilotSession,
        description: "You are a helpful assistant for CAE model comparison. " +
        "Your task is to analyze and compare the source and target model data provided in json format " +
        "and provide insights on the differences between them. " +
        "Focus on identifying new models added, models deleted, permission changes and any other significant changes. " +
        "Present your analysis in a clear and concise manner, " +
        "using tables where appropriate to highlight the differences." +
        "Always use a table for summary before presenting the analysis"
        );

    AgentSession session = await caeModelAgent.CreateSessionAsync();

    var userCredentials = new UserCredentials
    {
        UserName = ConsoleUi.PromptRequired("UserName"),
        Password = ConsoleUi.PromptRequired("Password", secret: true)
    };

    using var httpClientForWorkflow = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("cae_api_client");
    var authenticateExecutor = new AuthenticateExecutor(httpClientForWorkflow, memoryCache);
    var importProjectExecutor = new ImportProjectExecutor(httpClientForWorkflow, memoryCache);
    var modelDataExecutor = new ModelDataExecutor(httpClientForWorkflow);
    var tempJsonFileGeneratorExecutor = new TempJsonFileGeneratorExecutor(memoryCache);

    var workflow = new WorkflowBuilder(authenticateExecutor)
        .AddEdge(authenticateExecutor, importProjectExecutor)
        .AddEdge(importProjectExecutor, modelDataExecutor)
        .AddEdge(modelDataExecutor, tempJsonFileGeneratorExecutor)
        .WithOutputFrom(tempJsonFileGeneratorExecutor)
        .Build();

    var result = await InProcessExecution.RunAsync(workflow, userCredentials);

    foreach (WorkflowEvent evt in result.OutgoingEvents)
    {
        switch (evt)
        {
            case ExecutorFailedEvent failed:
                WriteError($"Step {failed.ExecutorId} failed: {failed.Data}");
                break;

            case WorkflowErrorEvent error:
                WriteError($"Workflow encountered an error: {error?.Exception?.Message}");
                break;

            case WorkflowOutputEvent output:
                if (await HandleWorkflowOutputAsync(output, caeModelAgent, session))
                {
                    return;
                }
                break;
        }
    }
}
catch (Exception ex)
{
    WriteError($"Program start up failed : {ex.Message}");
    return;
}
finally
{
    await CleanUpArtifacts();
}

async Task<bool> HandleWorkflowOutputAsync(WorkflowOutputEvent output, GitHubCopilotAgent caeModelAgent, AgentSession session)
{
    ConsoleUi.Clear();

    PrintStartupChecklist();

    var outputData = output.Data?.ToString();
    if (string.IsNullOrWhiteSpace(outputData))
    {
        WriteError("Workflow output data is null or empty.");
        return false;
    }

    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(outputData);
    if (data is null)
    {
        WriteError("Workflow output data could not be deserialized.");
        return false;
    }

    var sourceModelsFilePathKey = "sourceModels_FilePath".ToLower();
    if (!data.TryGetValue(sourceModelsFilePathKey, out var sourceModelsFilePath) || string.IsNullOrWhiteSpace(sourceModelsFilePath))
    {
        throw new InvalidOperationException("Workflow output is missing a valid source models file path.");
    }

    var targetModelsFilePathKey = "targetModels_FilePath".ToLower();
    if (!data.TryGetValue(targetModelsFilePathKey, out var targetModelsFilePath) || string.IsNullOrWhiteSpace(targetModelsFilePath))
    {
        throw new InvalidOperationException("Workflow output is missing a valid target models file path.");
    }

    ReadOnlyMemory<byte> sourceModelsFileContents = await File.ReadAllBytesAsync(sourceModelsFilePath);
    ReadOnlyMemory<byte> targetModelsFileContents = await File.ReadAllBytesAsync(targetModelsFilePath);

    while (true)
    {
        ConsoleUi.WriteBlankLine();
        var input = ConsoleUi.PromptOptional("You");

        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        var chatMessage = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, input);
        chatMessage.Contents.Add(new DataContent(sourceModelsFileContents, "application/json"));
        chatMessage.Contents.Add(new DataContent(targetModelsFileContents, "application/json"));

        await WriteSpectreResponseAsync(caeModelAgent.RunStreamingAsync(chatMessage, session));
    }

    return true;
}

async Task CleanUpArtifacts()
{
    try
    {
        using var httpClientForCleanup = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("cae_api_client");
        var token = memoryCache.Get<string>("AuthToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            WriteError("No auth token found in cache. Skipping cleanup of projects.");
            return;
        }
        var checkTokenResponse = await httpClientForCleanup.GetAsync("Authentication/login");
        if (checkTokenResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var userName = memoryCache.Get<string>("UserName")
                ?? throw new InvalidOperationException("UserName was not found in memory cache.");
            var password = memoryCache.Get<string>("Password") ?? throw new InvalidOperationException("Password was not found in memory cache.");

            var userCredentials = new UserCredentials
            {
                UserName = userName,
                Password = password
            };

            var authenticateResponse = await PostTokenRequestAsync("Authentication/login", userCredentials);
            var tokenData = await AuthenticateExecutor.ReadTokenResponseAsync(authenticateResponse, "Failed to retrieve token for cleanup.");
            token = tokenData.AccessToken;
            memoryCache.Set("AuthToken", tokenData.AccessToken);
            memoryCache.Set("RefreshToken", tokenData.RefreshToken);
            memoryCache.Set("TokenExpiration", DateTime.UtcNow);
        }
        httpClientForCleanup.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        if (!memoryCache.TryGetValue("SourceProjectId", out int sourceProjectId))
        {
            throw new InvalidOperationException("Source project id was not found in memory cache.");
        }

        if (!memoryCache.TryGetValue("TargetProjectId", out int cachedTargetProjectId))
        {
            throw new InvalidOperationException("Target project id was not found in memory cache.");
        }

        if (sourceProjectId != 0)
        {
            var deleteResponseSource = await httpClientForCleanup.DeleteAsync($"ProjectManagement/delete-project/?projectId={sourceProjectId}");
            deleteResponseSource.EnsureSuccessStatusCode();
            ConsoleUi.WriteSuccess($"Source project with ID {sourceProjectId} deleted successfully.");
        }

        if (cachedTargetProjectId != 0)
        {
            var deleteResponseTarget = await httpClientForCleanup.DeleteAsync($"ProjectManagement/delete-project/?projectId={cachedTargetProjectId}");
            deleteResponseTarget.EnsureSuccessStatusCode();
            ConsoleUi.WriteSuccess($"Target project with ID {cachedTargetProjectId} deleted successfully.");
        }
    }
    catch (Exception ex)
    {
        WriteError($"Error during cleanup: {ex.Message}");
    }
    finally
    {
        foreach (var cacheKey in new[] { "SourceModels_FilePath", "TargetModels_FilePath" })
        {
            if (memoryCache.TryGetValue<string>(cacheKey.ToLower(), out var tempFilePath) && File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
                ConsoleUi.WriteSuccess($"Temporary file {tempFilePath} deleted successfully.");
            }
        }
    }
}

async Task<HttpResponseMessage> PostTokenRequestAsync(string url, UserCredentials requestBody)
{
    var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    });
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    return await httpClient.PostAsync(url, content);
}

void PrintStartupChecklist()
{
    AnsiConsole.Write(new Rule("[bold cyan]CAE AI model comparison assistant[/]").RuleStyle("cyan"));

    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Cyan1)
        .AddColumn(new TableColumn("[bold]#[/]").Centered())
        .AddColumn(new TableColumn("[bold]Startup checklist[/]"));

    table.AddRow("1", "GitHub Copilot CLI exe should be present in [grey]C:\\Users\\{your_sesa_id}\\AppData\\Roaming\\vendor\\copilot\\copilot.exe[/].");
    table.AddRow("2", "CoPilot exe can be downloaded from - [link]https://github.com/github/copilot-cli/releases/tag/v0.0.410[/]");
    table.AddRow("3", "GitHub Copilot token should be set in the system environment variable named [bold]GITHUB_TOKEN[/].");
    table.AddRow("4", "CAE API should be running locally on [link]https://localhost:5001[/].");
    table.AddRow("5", "Valid username and password for the CAE API should be available for authentication when prompted.");
    table.AddRow("6", "Source and target folders should be present in [grey]C:\\Users\\{your_sesa_id}\\AppData\\Roaming\\cae_import_project[/].");
    table.AddRow("7", "Source and target exported projects should be present in the above folders.");

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
}

async Task WriteSpectreResponseAsync(IAsyncEnumerable<AgentResponseUpdate> updates)
{
    var responseBuilder = new StringBuilder();
    UsageDetails? usageDetails = null;
    List<AgentResponseUpdate> agentResponseUpdates = [];

    await AnsiConsole.Live(new Panel(new Markup("[grey]Waiting for response...[/]"))
            .Header("[bold cyan]Assistant[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1))
        .StartAsync(async ctx =>
        {
            await foreach (var run in updates)
            {
                agentResponseUpdates.Add(run);
                var text = run?.Text;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                responseBuilder.Append(text);
                ctx.UpdateTarget(
                    new Panel(new Markup(Markup.Escape(responseBuilder.ToString())))
                        .Header("[bold cyan]Assistant[/]")
                        .Border(BoxBorder.Rounded)
                        .BorderColor(Color.Cyan1)
                        .Expand());
            }
        });

    var updateAggregation = agentResponseUpdates.ToAgentResponse();
    usageDetails = updateAggregation.Usage;

    if (usageDetails is not null)
    {
        var usageTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn(new TableColumn("[bold yellow]Token Usage[/]"))
            .AddColumn(new TableColumn("[bold yellow]Count[/]").RightAligned());

#pragma warning disable MEAI001
        usageTable.AddRow("[grey]Input tokens[/]", $"[white]{usageDetails.InputTokenCount?.ToString() ?? "N/A"}[/]");
        usageTable.AddRow("[grey]Output tokens[/]", $"[white]{usageDetails.OutputTokenCount?.ToString() ?? "N/A"}[/]");
        usageTable.AddRow("[grey]Cached input tokens[/]", $"[white]{usageDetails.CachedInputTokenCount?.ToString() ?? "N/A"}[/]");
        usageTable.AddRow("[bold]Total tokens[/]", $"[bold white]{usageDetails.TotalTokenCount?.ToString() ?? "N/A"}[/]");
#pragma warning restore MEAI001

        AnsiConsole.Write(usageTable);
    }

    AnsiConsole.WriteLine();
}

void WriteError(string message)
{
    ConsoleUi.WriteError(message);
}


