using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class DocumentVersionDetailDto
    {
        [JsonPropertyName("data")]
        public object Data { get; set; } 

        [JsonPropertyName("filesName")]
        public List<string> FilesName { get; set; }

        [JsonPropertyName("binaries")]
        public List<string> Binaries { get; set; }

        // Otras propiedades que puedas necesitar (opcional)
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("documentVersionId")]
        public string DocumentVersionId { get; set; }
    }


}
