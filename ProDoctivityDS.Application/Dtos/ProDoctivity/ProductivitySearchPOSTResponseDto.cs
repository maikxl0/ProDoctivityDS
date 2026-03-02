using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class ProductivitySearchPOSTResponseDto
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageLength")]
        public int PageLength { get; set; }

        [JsonPropertyName("requestedPageLength")]
        public int RequestedPageLength { get; set; }

        [JsonPropertyName("totalRowCount")]
        public int TotalRowCount { get; set; }

        [JsonPropertyName("results")]
        public List<POSTDocumentDto>? Results { get; set; }
    }

    
}
