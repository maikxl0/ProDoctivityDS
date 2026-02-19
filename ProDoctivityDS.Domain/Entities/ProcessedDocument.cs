using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProDoctivityDS.Domain.Entities
{
    [Table("ProcessedDocuments")]
    public class ProcessedDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string DocumentId { get; set; } = string.Empty;

        [Required]
        public string OriginalFileName { get; set; } = string.Empty;

        public string? ProcessedFilePath { get; set; }
        public string? OriginalFilePath { get; set; }
        public int PagesRemoved { get; set; }
        public bool ApiUpdated { get; set; }
        public DateTime ProcessingDate { get; set; }
        public string? ErrorMessage { get; set; }
    }
}