using Enterprise_MAF_Samples.Models;
using Microsoft.Agents.AI.Workflows;

namespace Enterprise_MAF_Samples.WorkFlowUtils
{
    [SendsMessage(typeof(NameMatchSignal))]
    [YieldsOutput(typeof(string))]
    internal sealed class JudgeExecutor() : Executor<string>("judge")
    {
        private readonly string _targetName;
        private int _tries;

        /// <summary>
        /// Initializes a new instance of the <see cref="JudgeExecutor"/> class.
        /// </summary>
        /// <param name="targetName">The name to be guessed.</param>
        public JudgeExecutor(string targetName) : this()
        {
            this._targetName = targetName;
        }

        public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            this._tries++;
            if (message.Equals(this._targetName, StringComparison.Ordinal))
            {
                await context.YieldOutputAsync($"{this._targetName} found in {this._tries} tries!", cancellationToken);
            }
            else if (!string.Equals(message, this._targetName, StringComparison.Ordinal))
            {
                Console.WriteLine("Guess is incorrect!!!");
                await context.SendMessageAsync(NameMatchSignal.DidNotMatch, cancellationToken: cancellationToken);
            }
        }
    }
}
