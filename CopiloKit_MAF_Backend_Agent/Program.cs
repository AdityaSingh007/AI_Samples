using Azure.AI.OpenAI;
using CopiloKit_MAF_Backend_Agent.AgentFactory;
using CopiloKit_MAF_Backend_Agent.AgentTools;
using CopiloKit_MAF_Backend_Agent.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

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

AzureOpenAIClient client = new(new Uri(gptModelConfiguration.FoundryDeploymentUrl),
    new System.ClientModel.ApiKeyCredential(gptModelConfiguration.ModelToken));

var agent = ChatClientAgentFactory.CreateHumanInTheLoop(
    agentName: AgentName,
    modelName: "gpt-4.1-mini",
    client,
    tripPlannerTool: new AgentCustomTools());

// Register the created agent instance as a singleton service so it can be injected where needed.
builder.Services.AddSingleton<AIAgent>(agent);

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
app.MapAGUI("/human_in_the_loop", agent);

app.MapPost("/api/trips/approval-callback", (TripApprovalCallbackPayload payload) =>
{


    return TypedResults.Ok(new TripApprovalCallbackResponse(
        Enum.GetName(payload.Decision) ?? string.Empty,
        payload.Message,
        payload.Highlights,
        DateTimeOffset.UtcNow));
})
.WithName("TripApprovalCallback");

app.UseHttpsRedirection();

app.Run();
