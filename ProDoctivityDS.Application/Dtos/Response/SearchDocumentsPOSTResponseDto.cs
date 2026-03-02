using ProDoctivityDS.Application.Dtos.ProDoctivity;

namespace ProDoctivityDS.Application.Dtos.Response
{
    public class SearchDocumentsPOSTResponseDto
    {

        public List<POSTDocumentDto> Documents { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
    }
}
