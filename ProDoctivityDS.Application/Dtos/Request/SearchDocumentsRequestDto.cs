namespace ProDoctivityDS.Application.Dtos.Request
{
    public class SearchDocumentsRequestDto
    {
        public List<string>? DocumentTypeIds { get; set; }
        public string? Name { get; set; }
        public int Page { get; set; } = 0;
        public int RowsPerPage { get; set; } = 100;
    }
}
