using AGUICustomSdkServer.AgentTools;
using AGUICustomSdkServer.Interfaces;
using AGUICustomSdkServer.MinimalApiEndpoints;
using AGUICustomSdkServer.PdfIngestion;
using AGUICustomSdkServer.SearchServices;
using AGUICustomSdkServer.Services;
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
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddKeyedChatClient("se-qwen-chatClient", sp =>
{
    var qwenModelName = builder.Configuration["QwenModelName"];
    var modelUrl = builder.Configuration["ModelDeploymentUrl"];
    var qwenToken = Environment.GetEnvironmentVariable("qwen_Token", EnvironmentVariableTarget.User);
    if (string.IsNullOrWhiteSpace(qwenToken))
    {
        throw new InvalidOperationException("The user environment variable 'qwen_Token' is not configured.");
    }

    var openAIClient = new OpenAIClient(new ApiKeyCredential(qwenToken),
        new OpenAIClientOptions()
        {
            Endpoint = new Uri(modelUrl),
        });

    return openAIClient.GetChatClient(qwenModelName).AsIChatClient();
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
builder.Services.AddSingleton<IDocumentExtraction, DocumentExtraction>();
builder.Services.AddSingleton<AgentTool>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

//app.MapPdfEndpoints();
app.MapDocumentExtractionEndpoints();


var chatClient = app.Services.GetRequiredKeyedService<IChatClient>("se-qwen-chatClient");
var agentTool = app.Services.GetRequiredService<AgentTool>();
//var azureOpenAIChatClient = app.Services.GetRequiredKeyedService<IChatClient>("AzureOpenAIChatClient");

AIAgent seAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    Name = "AGUIAssistant",
    ChatOptions = new()
    {
        Instructions = "You are a helpful assistant. " +
        "For any user query related to car lease policy, invoke the GetCarleaseKnowledgeTree tool before answering. " +
        "Use the relevant policy information returned by the tool to answer the query. " +
        "If the tool does not return sufficient information, clearly state what information is unavailable and do not invent policy details.",
        Tools =
        [
            //AIFunctionFactory.Create(agentTool.SearchQwenKnowledge),
            AIFunctionFactory.Create(agentTool.GetCarleaseKnowledgeTree)
        ],
    }
});

//AIAgent azureOpenAIAgent = azureOpenAIChatClient.AsAIAgent(
//    name: "AzureOpenAIAssistant",
//    instructions: "You are a helpful assistant using Azure OpenAI.");

app.MapAGUIServer("/test-assistant", seAgent);



app.Run();