using Enterprise_MAF_Samples.Models;
using Microsoft.Agents.AI.Workflows;

namespace Enterprise_MAF_Samples.WorkFlowUtils
{
    internal static class WorkflowFactory
    {
        internal static Workflow BuildWorkflow()
        {
            // Create the executors
            RequestPort nameRequestPort = RequestPort.Create<NameMatchSignal, string>("GuessName");
            JudgeExecutor judgeExecutor = new("Test");

            // Build the workflow by connecting executors in a loop
            return new WorkflowBuilder(nameRequestPort)
                .AddEdge(nameRequestPort, judgeExecutor)
                .AddEdge(judgeExecutor, nameRequestPort)
                .WithOutputFrom(judgeExecutor)
                .Build();
        }
    }
}
