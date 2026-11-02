using System.Text.Json.Serialization;

namespace AGUICustomSdkServer.Models
{
    public record DocumentNode(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("referenceLocation")] string ReferenceLocation // Page number, Row index, or Section header
);
}
