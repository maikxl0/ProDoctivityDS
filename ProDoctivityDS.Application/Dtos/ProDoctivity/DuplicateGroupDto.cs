namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class DuplicateGroupDto
    {
        public string GroupKey { get; set; } = string.Empty; // Ej: "TypeId_Hash"
        public List<DuplicateDocumentDto> Documents { get; set; } = new();
        public string Reason { get; set; } = string.Empty; // Ej: "Mismo tipo y mismo hash"
    }
}
