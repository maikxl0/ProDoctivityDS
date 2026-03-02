using ProDoctivityDS.Application.Dtos.ProDoctivity;
using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.Response
{
    public class ProductivityDocumentResponse
    {
        [JsonPropertyName("document")]
        public ProductivityDocumentDto Document { get; set; } = new();
    }
}
