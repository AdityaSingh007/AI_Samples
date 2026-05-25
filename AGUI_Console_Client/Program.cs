using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using System.Text;

using HttpClient httpClient = new()
{
    Timeout = TimeSpan.FromSeconds(60)
};
const string serverUrl = "http://localhost:5026";
ConsoleColor textColor = ConsoleColor.Yellow;
AGUIChatClient chatClient = new AGUIChatClient(httpClient, serverUrl);
AIAgent agent = chatClient.AsAIAgent(name: "agui-client",
    description: "AG-UI Client Agent");

AgentSession session = await agent.CreateSessionAsync();

List<ChatMessage> messages = [new ChatMessage(ChatRole.System, "You are a nice AI Agent")];
while (true)
{
    Console.Write("> ");
    string message = Console.ReadLine() ?? string.Empty;
    if (message == string.Empty)
    {
        continue;
    }

    messages.Add(new ChatMessage(ChatRole.User, message));

    List<AgentResponseUpdate> updates = [];
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messages, session))
    {
        updates.Add(update);
        foreach (AIContent content in update.Contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    Console.ForegroundColor = textColor;
                    Console.Write(textContent.Text);
                    break;

                case FunctionCallContent functionCallContent:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    StringBuilder toolCallDetails = new();
                    toolCallDetails.Append($"[Tool Call: {functionCallContent.Name}");
                    if (functionCallContent.Arguments != null && functionCallContent.Arguments.Any())
                    {
                        toolCallDetails.Append($" (Args: {string.Join(",", functionCallContent.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
                    }

                    toolCallDetails.Append("]");
                    Console.WriteLine(toolCallDetails);
                    Console.ForegroundColor = textColor;
                    break;
                case FunctionResultContent functionResultContent:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    bool isError = functionResultContent.Exception != null;
                    Console.WriteLine(isError ? $"[Tool Error: {functionResultContent.Exception}]" : $"[Tool Result: {functionResultContent.Result}]");

                    Console.ForegroundColor = textColor;
                    break;

                case ErrorContent errorContent:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[Error: {errorContent.Message}]");
                    Console.ForegroundColor = textColor;
                    break;
            }
        }

        Console.ResetColor();
    }

    AgentResponse fullResponse = updates.ToAgentResponse();
    messages.AddRange(fullResponse.Messages);

    Console.WriteLine();
    Console.WriteLine();
}