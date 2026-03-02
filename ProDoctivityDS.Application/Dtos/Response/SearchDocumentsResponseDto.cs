namespace ProDoctivityDS.Application.Dtos.Response
{
    public class SearchDocumentsResponseDto
    {
        public List<DocumentDto> Documents { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
    }
    
}
