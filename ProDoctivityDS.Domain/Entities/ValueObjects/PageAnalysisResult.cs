namespace ProDoctivityDS.Application.Dtos.ValueObjects
{
    public class PageAnalysisResult
    {
        public bool ShouldRemove { get; set; }
        public string Diagnosis { get; set; }
        public string ExtractedTextPreview { get; set; } // Primeros 200 caracteres, por ejemplo
        public int PageNumber { get; set; } // 1-based para logs
    }
}
