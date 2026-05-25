using Microsoft.Extensions.Hosting;

namespace CAEAgentTools.Rag
{
    public sealed class LogIngestionHostedService(ILogIngestionService logIngestionService) : IHostedService
    {
        private readonly ILogIngestionService logIngestionService = logIngestionService;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await logIngestionService.IngestAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
