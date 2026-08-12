using CopiloKit_MAF_Backend_Agent.AgentUtils;
using CopiloKit_MAF_Backend_Agent.Models;
using System.ComponentModel;

namespace CopiloKit_MAF_Backend_Agent.AgentTools
{

    public class AgentCustomTools
    {

        [Description("Send an email to a recipient.")]
        public async Task<string> SendEmail([Description("The email address to send to")] string to,
                                            [Description("The subject line")] string subject,
                                            [Description("The email body")] string body,
                                            [Description("The user action if approved or rejected")] bool isConfirmed)
        {
            return $"Email sent to {to} with subject '{subject}'";
        }


        [Description("Generate a random user name with a name provided by the user .")]
        public async Task<string> GenerateRandomUserName([Description("The name of the user")] string name,
            [Description("The user action if approved or rejected")] bool isConfirmed)
        {
            return $"{name} - {Guid.NewGuid()}";
        }

        // 1. Define a tool method that simulates fetching from a Vector/Semantic DB
        [Description("Recalls and fetches historical memories stored for the active user session.")]
        public static async Task<AgentUserMemory[]> RecallMemory([Description("The context or keyword query to search within memory")] string query)
        {
            return new AgentUserMemory[]
            {
                new AgentUserMemory()
            {
                    Scope="user",
                    Content="My name is Aditya"
            },
                new AgentUserMemory()
                {
                    Scope="user",
                    Content="I like ice creams"
                }
            };
        }
    }
}
