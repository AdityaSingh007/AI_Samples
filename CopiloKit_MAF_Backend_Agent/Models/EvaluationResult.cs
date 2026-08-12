using System.ComponentModel;

namespace CopiloKit_MAF_Backend_Agent.Models
{
    public class EvaluationResult
    {
        [Description("True if the text directly answers the user query. False if it asks a clarifying question, admits it doesn't know, refuses, or errors out.")]
        public bool IsAnswer { get; set; }

        [Description("A brief reason behind the classification.")]
        public string Reasoning { get; set; } = string.Empty;
    }
}
