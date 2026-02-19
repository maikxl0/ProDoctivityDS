namespace ProDoctivityDS.Application.Dtos.Response
{
    public class ProcessProgressDto
    {
        public int Total { get; set; }
        public int Processed { get; set; }
        public int Updated { get; set; }
        public int PagesRemoved { get; set; }
        public int Errors { get; set; }
        public int Skipped { get; set; }
        public string CurrentDocumentName { get; set; } = string.Empty;
        public string? CurrentDocumentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public double PercentComplete => Total == 0 ? 0 : (double)Processed / Total * 100;
    }
}