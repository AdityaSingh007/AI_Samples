using AGUICustomSdkServer.AgentTools;
using AGUICustomSdkServer.PdfIngestion;
using AGUICustomSdkServer.SearchServices;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton(sp =>
{
    var qwenModelName = builder.Configuration["QwenModelName"];
    var modelUrl = builder.Configuration["ModelDeploymentUrl"];
    var openAIClient = new OpenAIClient(new ApiKeyCredential("0e59c7673a7197dd6191045edfcdaf36dce2311328a6dce4eff77e01c50ebb55"),
        new OpenAIClientOptions()
        {
            Endpoint = new Uri(modelUrl),
        });

    return openAIClient.GetChatClient(qwenModelName);
});

builder.Services.AddKeyedChatClient("AzureOpenAIChatClient", sp =>
{
    var foundryProjectEndpoint = builder.Configuration["Foundry_Deployment_Url"];
    var gpt_4_1_Token = builder.Configuration["gpt_4_1_Mini_Token"];
    if (string.IsNullOrWhiteSpace(foundryProjectEndpoint) || string.IsNullOrWhiteSpace(gpt_4_1_Token))
    {
        throw new InvalidOperationException("Foundry project endpoint or GPT-4.1 token is not configured.");
    }

    AzureOpenAIClient client = new(new Uri(foundryProjectEndpoint),
        new System.ClientModel.ApiKeyCredential(gpt_4_1_Token));

    return client.GetChatClient("gpt-4.1-mini").AsIChatClient();

});

builder.Services.AddEmbeddingGenerator(sp =>
{
    var foundryProjectEndpoint = builder.Configuration["Foundry_Deployment_Url"];
    var gpt_4_1_Token = builder.Configuration["gpt_4_1_Mini_Token"];
    if (string.IsNullOrWhiteSpace(foundryProjectEndpoint) || string.IsNullOrWhiteSpace(gpt_4_1_Token))
    {
        throw new InvalidOperationException("Foundry project endpoint or GPT-4.1 token is not configured.");
    }

    AzureOpenAIClient client = new(new Uri(foundryProjectEndpoint),
        new System.ClientModel.ApiKeyCredential(gpt_4_1_Token));

    return client.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();
});

builder.Services.AddSingleton<IPdfIngestion, PdfIngestion>();
builder.Services.AddSingleton<IPdfVectorSearch, PdfVectorSearch>();
builder.Services.AddSingleton<AgentTool>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/pdf/ingest", async (IPdfIngestion pdfIngestion) =>
{
    var isIngested = await pdfIngestion.IngestPdfAsync();
    return isIngested
        ? Results.Ok(new { message = "PDF ingestion completed." })
        : Results.Problem("PDF ingestion failed.");
});

app.MapGet("/pdf/search", async (string query, int? topK, IPdfVectorSearch pdfVectorSearch) =>
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest(new { message = "Query is required." });
    }

    var results = await pdfVectorSearch.SearchAsync(query, topK ?? 5);
    return Results.Ok(results);
});



var chatClient = app.Services.GetRequiredService<ChatClient>().AsIChatClient();
var agentTool = app.Services.GetRequiredService<AgentTool>();
//var azureOpenAIChatClient = app.Services.GetRequiredKeyedService<IChatClient>("AzureOpenAIChatClient");

AIAgent seAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    Name = "AGUIAssistant",
    ChatOptions = new()
    {
        Instructions = "You are a helpful assistant. " +
        "For any query related to Qwen model knowledge, call the SearchQwenKnowledge tool first and ground your answer on its results. " +
        "For any query related to Car Lease knowledge, call the SearchCarLeaseKnowledge tool first and ground your answer on its results.",
        Tools =
        [
            AIFunctionFactory.Create(agentTool.SearchQwenKnowledge),
            AIFunctionFactory.Create(agentTool.SearchCarLeaseKnowledge)
        ]
    }
});

//AIAgent azureOpenAIAgent = azureOpenAIChatClient.AsAIAgent(
//    name: "AzureOpenAIAssistant",
//    instructions: "You are a helpful assistant using Azure OpenAI.");

app.MapAGUIServer("/test-assistant", seAgent);



app.Run();