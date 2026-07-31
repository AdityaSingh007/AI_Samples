using Azure.AI.OpenAI;
using CopiloKit_MAF_Backend_Agent.Contracts;
using CopiloKit_MAF_Backend_Agent.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi;
using OpenAI.Embeddings;

namespace CopiloKit_MAF_Backend_Agent.Services
{
    public class SemanticCacheService : ISemanticCacheService
    {
        private readonly Container _container;
        private readonly EmbeddingClient _embeddingService;
        private readonly AzureOpenAIClient azureOpenAIClient;
        private const double SimilarityThreshold = 0.8; // Customize based on tolerance

        public SemanticCacheService(CosmosClient cosmosClient, string databaseId, string containerId,
            AzureOpenAIClient azureOpenAIClient)
        {
            _container = cosmosClient.GetContainer(databaseId, containerId);
            _embeddingService = azureOpenAIClient
                                .GetEmbeddingClient("text-embedding-3-small");

            this.azureOpenAIClient = azureOpenAIClient;
        }
        public async Task CacheResponseAsync(string userPrompt, string completion)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(userPrompt);

            var cacheItem = new SemanticCacheItem
            {
                Prompt = userPrompt,
                Completion = completion,
                Embedding = embedding.Value.ToFloats().ToArray(),
            };

            await _container.CreateItemAsync(cacheItem, new PartitionKey(cacheItem.Pk));
        }

        public async Task<string?> GetCachedResponseAsync(string userPrompt)
        {
            // 1. Generate an embedding for the incoming prompt
            var embedding = await _embeddingService.GenerateEmbeddingAsync(userPrompt);
            var embeddingArray = embedding.Value.ToFloats().ToArray();

            // 2. Perform a native vector search inside Cosmos DB
            string queryText = @"
            SELECT TOP 1 c.id,
                         c.pk, 
                         c.Prompt, 
                         c.Completion, 
                         c.Embedding,
                         VectorDistance(c.Embedding, @embedding) AS SimilarityScore
            FROM c
            WHERE VectorDistance(c.Embedding, @embedding) >= @threshold
            ORDER BY VectorDistance(c.Embedding, @embedding)";

            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@embedding", embeddingArray)
                .WithParameter("@threshold", SimilarityThreshold);

            var requestOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey("cache")
            };

            using var iterator = _container.GetItemQueryIterator<SemanticCacheResponse>(
                queryDefinition);
            try
            {
                //while (iterator.HasMoreResults)
                //{
                //    FeedResponse<SemanticCacheResponse> response = await iterator.ReadNextAsync();
                //    foreach (SemanticCacheResponse item in response)
                //    {
                //        Console.WriteLine($"Similarity Score: {item.SimilarityScore}");
                //        Console.WriteLine($"Title: {item.Id}");
                //        Console.WriteLine($"Description: {item.Prompt}");
                //        Console.WriteLine("=====================================");

                //    }
                //}
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var matchedItem = response.FirstOrDefault();
                    return matchedItem?.Completion;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }
    }
}
