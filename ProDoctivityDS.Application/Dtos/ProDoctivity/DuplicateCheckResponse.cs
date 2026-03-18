namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class DuplicateCheckResponse
    {
        public List<DuplicateGroupDto> Groups { get; set; } = new();
        public int TotalDocuments { get; set; }
    }
}
