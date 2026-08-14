using Azure.AI.OpenAI;
using CopiloKit_MAF_Backend_Agent.AgentFactory;
using CopiloKit_MAF_Backend_Agent.AgentTools;
using CopiloKit_MAF_Backend_Agent.ContextHistoryProvider;
using CopiloKit_MAF_Backend_Agent.Contracts;
using CopiloKit_MAF_Backend_Agent.Models;
using CopiloKit_MAF_Backend_Agent.Services;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.AGUI;

const string AgentName = "human_in_the_loop";
var builder = WebApplication.CreateBuilder(args);

var gptModelConfiguration = new GptModelConfiguration();
builder.Configuration.GetSection("gptModel").Bind(gptModelConfiguration);

var gpt_4_1_Token = builder.Configuration["gpt_4_1_Mini_Token"];

if (string.IsNullOrEmpty(gpt_4_1_Token))
{
    Console.WriteLine("GPT-4.1 token is not configured in environment variables.");
    return;
}

if (gptModelConfiguration is null)
{
    Console.WriteLine("Failed to load GPT model configuration.");
    return;
}

if (string.IsNullOrEmpty(gptModelConfiguration.FoundryDeploymentUrl))
{
    Console.WriteLine("Foundry Deployment URL is not configured.");
    return;
}

gptModelConfiguration.ModelToken = gpt_4_1_Token;

var foundryProjectEndpoint = builder.Configuration["FOUNDRY_PROJECT_ENDPOINT"];
var apiKey = builder.Configuration["FOUNDRY_PROJECT_API_KEY"];
var tenantId = builder.Configuration["TENANT_ID"];
var clientId = builder.Configuration["CLIENT_ID"];
var clientSecret = builder.Configuration["CLIENT_SECRET"];

if (string.IsNullOrWhiteSpace(foundryProjectEndpoint) 
    || string.IsNullOrWhiteSpace(apiKey) 
    || string.IsNullOrWhiteSpace(tenantId) 
    || string.IsNullOrWhiteSpace(clientId) 
    || string.IsNullOrWhiteSpace(clientSecret))
{
    Console.WriteLine("Foundry project endpoint, API key, or Azure AD credentials are not configured.");
    return;
}

AzureOpenAIClient client = new(new Uri(gptModelConfiguration.FoundryDeploymentUrl),
    new System.ClientModel.ApiKeyCredential(gptModelConfiguration.ModelToken));

builder.Services.AddSingleton<IAgentCosmosChatHistoryProvider, AgentCosmosChatHistoryProvider>();

//var agent = await ChatClientAgentFactory.CreateHumanInTheLoopWithMiddleware(
//    agentName: AgentName,
//    modelName: "gpt-4.1-mini",
//    client,
//    agentCustomTools: new AgentCustomTools(),
//    foundryProjectEndpoint: foundryProjectEndpoint,
//    apiKey: apiKey,
//    tenantId: tenantId,
//    clientId: clientId,
//    clientSecret: clientSecret
//);

var cosmosEndpoint = builder.Configuration["COSMOS_ENDPOINT"] ?? throw new InvalidOperationException("COSMOS_ENDPOINT is not configured.");
var cosmosKey = builder.Configuration["COSMOS_KEY"] ?? throw new InvalidOperationException("COSMOS_KEY is not configured.");
var semanticCacheDatabase = builder.Configuration["SEMANTIC_CACHE_DATABASE"] ?? "CosmosDBForMyAIAgent";
var semanticCacheContainer = builder.Configuration["SEMANTIC_CACHE_CONTAINER"] ?? "cache";

var semanticCacheService = new SemanticCacheService(
    new CosmosClient(cosmosEndpoint, cosmosKey, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway }),
    semanticCacheDatabase,
    semanticCacheContainer,
    client);

var agentWithMemory = await ChatClientAgentFactory.CreateAgentWithMemory(
    agentName: AgentName,
    modelName: "gpt-4.1-mini",
    client,
    new AgentCosmosChatHistoryProvider(builder.Configuration),
    semanticCacheService
);

// Register the created agent instance as a singleton service so it can be injected where needed.
//builder.Services.AddSingleton<AIAgent>(agent);
builder.Services.AddSingleton(agentWithMemory);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLocalhostOrigins");
app.MapAGUI("/human_in_the_loop", agentWithMemory);

app.UseHttpsRedirection();

app.Run();
