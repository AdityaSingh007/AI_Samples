using CAE_Log_Agent.Utilities;
using CAEAgentTools.AgentTools;
using CAEAgentTools.Rag;
using GitHub.Copilot.SDK;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.GitHub.Copilot;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

await FolderUtility.EmptyAgentTempFolder();

const string AgentName = "cae-log-agent";
var databasePath = Path.Combine("C:\\CAE_Release_Installers\\win-x64\\logs", "logs.db3");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogRagServices(databasePath);

var cliPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vendor", "copilot", "copilot.exe");

var gitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN", EnvironmentVariableTarget.User)
           ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set. Please set it in your user environment variables and try again.");

await using CopilotClient copilotClient = new(new CopilotClientOptions()
{
    CliPath = cliPath,
    GitHubToken = gitHubToken,
    UseLoggedInUser = false
});

await copilotClient.StartAsync();

builder.Services.AddSingleton(copilotClient);
builder.Services.AddSingleton<GitHubCopilotAgent>(sp =>
{
    var agentLogTool = sp.GetRequiredService<AgentLogTool>();

    var gitHubCoPilotSession = new SessionConfig()
    {
        Model = "GPT-5.4",
        Streaming = true,
        OnPermissionRequest = PermissionHandler.ApproveAll,
        InfiniteSessions = new InfiniteSessionConfig { Enabled = true },
        Tools =
        [
            AIFunctionFactory.Create(agentLogTool.SearchLogs)
        ]
    };

    return new GitHubCopilotAgent(
        copilotClient,
        gitHubCoPilotSession,
        name: AgentName,
        description: "You are a log analyzer agent. " +
        "Your task is to inspect application logs, identify errors, warnings, anomalies, " +
        "and recurring failure patterns, and explain likely root causes in clear operational terms. " +
        "Whenever a user requests log analysis, always call the SearchLogs tool first to retrieve grounded log context for the requested date range. " +
        "Use the SearchLogs tool when the user asks for semantic matching, similar incidents, or log entries related to a described issue. " +
        "If a user attaches a log file, analyze the attached file directly first. " +
        "Use only the retrieved log context as evidence, cite timestamps, affected areas, and call sites, and do not invent facts that are not present in the logs. " +
        "Keep the response simple and not very technical in nature.");
});

builder.Services.AddSingleton<AIAgent>(sp => sp.GetRequiredService<GitHubCopilotAgent>());
builder.Services.AddKeyedSingleton<AIAgent>(AgentName, (sp, _) => sp.GetRequiredService<GitHubCopilotAgent>());
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowLocalhostOrigins",
        policy =>
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();
builder.Services.AddDevUI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapOpenAIResponses();
    app.MapOpenAIConversations();
    app.MapDevUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhostOrigins");
app.MapAGUI(AgentName, "/");
app.Run();