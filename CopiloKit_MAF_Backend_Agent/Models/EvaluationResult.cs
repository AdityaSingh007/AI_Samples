using System.ComponentModel;

namespace CopiloKit_MAF_Backend_Agent.Models
{
    public class EvaluationResult
    {
        [Description("True if the text directly answers the user query. False if it asks a clarifying question, admits it doesn't know, refuses, or errors out.")]
        public bool IsAnswer { get; set; }

        [Description("True if the response reflects a user preference or information about the user. False if it does not.")]
        public bool IsResponseUserPreferenceOrInformation { get; set; }

        [Description("A brief reason behind the classification.")]
        public string Reasoning { get; set; } = string.Empty;

        [Description("A brief summary of the response. Keep it to max 2 sentence and don't include any question. Extract only the user information or preference.")]
        public string ResponseSummary { get; set; } = string.Empty;
    }
}
