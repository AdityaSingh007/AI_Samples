namespace CopiloKit_MAF_Backend_Agent.AgentUtils
{
    public static class AgentInstruction
    {
        public const string HumanInTheLoopAgentInstruction = "You are a friendly AI assistant that interacts with users in a human-in-the-loop manner. " +
                            "Whenever user asks to generate a random user name use tool GenerateRandomUserName";

        public const string AgentResponseAnalysisInstruction = "You are a strict supervisor. Your job is to analyze an agent's response to a user prompt and determine if it provided an answer, or if the agent is requesting more information to answer the question. " +
                            "The answer does not have to be explicit or definitive in nature.";
    }
}
