namespace ProDoctivityDS.Application.Dtos.Request
{
    public class TestAnalysisRequestDto
    {
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}