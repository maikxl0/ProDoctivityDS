using ProDoctivityDS.Application.Dtos.ProDoctivity;
using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.Response
{
    public class DocumentTypeListResponse
    {
        [JsonPropertyName("documentTypes")]
        public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    }
}
