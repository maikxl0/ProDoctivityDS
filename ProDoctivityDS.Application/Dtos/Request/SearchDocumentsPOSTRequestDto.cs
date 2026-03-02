using System.ComponentModel.DataAnnotations;

namespace ProDoctivityDS.Application.Dtos.Request
{
    public class SearchDocumentsPOSTRequestDto
    {
        public List<string>? DocumentTypeId { get; set; }
        public string? Name { get; set; }
        public int Page { get; set; } = 0;
        [Range(15, 100, ErrorMessage = "RowsPerPage debe ser 15, 30 o 100")]
        public int RowsPerPage { get; set; } = 100;
    }
}
