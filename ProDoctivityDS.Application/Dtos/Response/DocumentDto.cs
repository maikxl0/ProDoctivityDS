namespace ProDoctivityDS.Application.Dtos.Response
{
    public class DocumentDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DocumentTypeName { get; set; } = string.Empty;
        public string DocumentTypeId { get; set; } = string.Empty;
        public long CreatedAt { get; set; } // Timestamp Unix
        public int? PageCount { get; set; }
        public string AnalysisStatus { get; set; } = "Pendiente"; // Pendiente, Removida, Conservada
    }
}
