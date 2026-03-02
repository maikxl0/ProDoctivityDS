using ProDoctivityDS.Application.Dtos.ProDoctivity;
using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.Response
{
    public class DocumentVersionsResponse
    {
        [JsonPropertyName("documentVersions")]
        public List<ProductivityVersionDto> DocumentVersions { get; set; } = new();
    }
}
