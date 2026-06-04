using Enterprise_MAF_Samples.Models;
using Microsoft.Agents.AI.Workflows;

namespace Enterprise_MAF_Samples.WorkFlowUtils
{
    internal class HITLLoop
    {
        public static async Task Main()
        {
            // Create the workflow
            var workflow = WorkflowFactory.BuildWorkflow();

            // Execute the workflow
            await using StreamingRun handle = await InProcessExecution.RunStreamingAsync(workflow, NameMatchSignal.Init);
            await foreach (WorkflowEvent evt in handle.WatchStreamAsync())
            {
                switch (evt)
                {
                    case RequestInfoEvent requestInputEvt:
                        // Handle `RequestInfoEvent` from the workflow
                        ExternalResponse response = HandleExternalRequest(requestInputEvt.Request);
                        await handle.SendResponseAsync(response);
                        break;

                    case WorkflowOutputEvent outputEvt:
                        // The workflow has yielded output
                        Console.WriteLine($"Workflow completed with result: {outputEvt.Data}");
                        return;

                    case WorkflowErrorEvent workflowError:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error occurred.");
                        Console.ResetColor();
                        return;

                    case ExecutorFailedEvent executorFailed:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine($"Executor '{executorFailed.ExecutorId}' failed with {(executorFailed.Data == null ? "unknown error" : $"exception {executorFailed.Data}")}.");
                        Console.ResetColor();
                        return;
                }
            }
        }


        public static ExternalResponse HandleExternalRequest(ExternalRequest request)
        {
            if (request.TryGetDataAs<NameMatchSignal>(out var signal))
            {
                switch (signal)
                {
                    case NameMatchSignal.Init:
                        string initialGuess = ReadStringFromConsole("Please provide your initial guess: ");
                        return request.CreateResponse(initialGuess);

                    case NameMatchSignal.DidNotMatch:
                        string againGuess = ReadStringFromConsole("Please provide your next guess: ");
                        return request.CreateResponse(againGuess);
                }
            }

            throw new NotSupportedException($"Request {request.PortInfo.RequestType} is not supported");
        }

        public static string ReadStringFromConsole(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    return input;
                }
                Console.WriteLine("Invalid input. Please enter a valid integer.");
            }
        }
    }
}
