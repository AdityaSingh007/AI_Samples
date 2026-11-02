using System.Text.Json.Serialization;

namespace AGUICustomSdkServer.Models
{
    public record DocumentStructureTree(
    [property: JsonPropertyName("documentTitle")] string DocumentTitle,
    [property: JsonPropertyName("sections")] List<DocumentNode> Sections
);
}
