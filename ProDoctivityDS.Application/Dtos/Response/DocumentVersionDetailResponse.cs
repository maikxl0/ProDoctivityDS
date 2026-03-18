using ProDoctivityDS.Application.Dtos.ProDoctivity;
using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.Response
{
    public class DocumentVersionDetailResponse
    {
        [JsonPropertyName("document")]
        public DocumentVersionDetailDto Document { get; set; } = new();
    }
}
