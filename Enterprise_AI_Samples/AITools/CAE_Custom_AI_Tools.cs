using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CAE_AI_Samples.AITools
{
    public sealed class CAEProjectImportRequest
    {
        public required string ProjectPath { get; init; }
        public required string Password { get; init; }
        public required bool UpgradeModelDefinition { get; init; }
    }

    public sealed class CAEProjectImportResponse
    {
        [JsonPropertyName("project")]
        public CAEProjectDetails? Project { get; init; }
    }

    public sealed class CAEProjectDetails
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("architectureType")]
        public string? ArchitectureType { get; init; }

        [JsonPropertyName("modificationDate")]
        public DateTimeOffset? ModificationDate { get; init; }

        [JsonPropertyName("modifiedBy")]
        public string? ModifiedBy { get; init; }

        [JsonPropertyName("creationDate")]
        public DateTimeOffset? CreationDate { get; init; }

        [JsonPropertyName("createdBy")]
        public string? CreatedBy { get; init; }

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; init; }

        [JsonPropertyName("isCorrupted")]
        public bool? IsCorrupted { get; init; }

        [JsonPropertyName("isProjectFileExists")]
        public bool? IsProjectFileExists { get; init; }

        [JsonPropertyName("enableDeviceModelManagement")]
        public bool EnableDeviceModelManagement { get; init; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; init; }

        [JsonPropertyName("path")]
        public string? Path { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }
    }

    public static class CAE_Custom_AI_Tools
    {
        
    }
}
