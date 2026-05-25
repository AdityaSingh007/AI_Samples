using CAE_AI_Samples.AITools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CAE_AI_Samples.Models
{
    public sealed class ProjectImportResponse
    {
        [JsonPropertyName("project")]
        public CAEProjectDetails? Project { get; init; }
    }
}
