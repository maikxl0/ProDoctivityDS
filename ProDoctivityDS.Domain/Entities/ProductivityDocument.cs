namespace ProDoctivityDS.Domain.Entities
{
    public class ProductivityDocument
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DocumentTypeId { get; set; } = string.Empty;
        public string DocumentTypeName { get; set; } = string.Empty;
        public string? LastDocumentVersionId { get; set; }
        public long CreatedAt { get; set; }
        public int? PageCount { get; set; }
        public AnalysisStatus AnalysisStatus { get; set; } = AnalysisStatus.Pending;
    }

    public enum AnalysisStatus
    {
        Pending,
        Removed,
        Preserved
    }
}
