using CAEAgentTools.AgentTools;
using CAEAgentTools.VectorStore;
using EnterpriseAgentTools.VectorStore;
using Microsoft.Extensions.DependencyInjection;

namespace CAEAgentTools.Rag
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogRagServices(this IServiceCollection services, string databasePath)
        {
            services.AddSingleton<ILogRepository>(_ => new SqliteLogRepository(databasePath));
            services.AddSingleton<ILogVectorStore, SqliteVectorStoreProvider>();
            services.AddSingleton<ILogIngestionService, LogIngestionService>();
            services.AddSingleton<ILogRagService, LogRagService>();
            services.AddSingleton<AgentLogTool>();
            services.AddHostedService<LogIngestionHostedService>();

            return services;
        }
    }
}
