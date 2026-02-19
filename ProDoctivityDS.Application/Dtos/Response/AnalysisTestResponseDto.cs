namespace ProDoctivityDS.Application.Dtos.Response
{
    public class AnalysisTestResponseDto
    {
        public bool ShouldRemove { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? NormalizedText { get; set; }
    }
}