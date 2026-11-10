using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AGUI_Console_Client
{
    internal static class SampleClient
    {
        public static async Task RunAsync(
        IChatClient chatClient,
        TextWriter output,
        byte[]? fileBytes = null,
        string fileName = "",
        string fileMimeType = "application/octet-stream",
        List<List<ChatMessage>>? messagesPerTurn = null,
        List<List<ChatResponseUpdate>>? updatesPerTurn = null,
        CancellationToken cancellationToken = default)
        {
            var bytes = fileBytes;
            var fileData = new DataContent(bytes, fileMimeType)
            {
                Name = fileName,
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    ["metadata"] = JsonSerializer.SerializeToElement(new
                    {
                        filename = fileName,
                        //detail = "high",
                        //providerHint = new { quality = "original" }
                    })
                }
            };
            var messages = new List<ChatMessage>
        {
            new(ChatRole.User,
            [
                new TextContent("Compare IEMI rank and score for karantaka and tamil nadu and tell me which state is leading ?"),
                //fileData,
            ]),
        };
            messagesPerTurn?.Add(messages.ToList());
            await output.WriteLineAsync($"> Compare IEMI rank and score for karantaka and tamil nadu and tell me which state is leading ? ({bytes.Length} bytes)").ConfigureAwait(false);

            var updates = new List<ChatResponseUpdate>();
            await foreach (var update in chatClient.GetStreamingResponseAsync(
                messages, options: null, cancellationToken).ConfigureAwait(false))
            {
                updates.Add(update);
                if (!string.IsNullOrEmpty(update.Text))
                {
                    await output.WriteAsync(update.Text).ConfigureAwait(false);
                }
            }
            await output.WriteLineAsync().ConfigureAwait(false);
            updatesPerTurn?.Add(updates);
        }
    }
}
