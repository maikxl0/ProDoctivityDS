using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{

    public class DocumentVersionDetailResponse
    {
        [JsonPropertyName("document")]
        public DocumentVersionDetailDto Document { get; set; } = new();
    }

    public class DocumentVersionDetailDto
    {
        [JsonPropertyName("binaries")]
        public List<string> Binaries { get; set; } = new();

        // Opcional: otras propiedades que necesites (por ejemplo, documentId, name, etc.)
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("documentVersionId")]
        public string DocumentVersionId { get; set; } = string.Empty;
    }


}
