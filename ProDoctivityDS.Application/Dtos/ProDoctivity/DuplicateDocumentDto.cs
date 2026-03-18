namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class DuplicateDocumentDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DocumentTypeId { get; set; } = string.Empty;
        public string DocumentTypeName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? FileHash { get; set; } // Opcional
        public string CreatedAt { get; set; }
        public string GroupKey { get; set; } = string.Empty; // Clave para agrupar duplicados
    }
}
