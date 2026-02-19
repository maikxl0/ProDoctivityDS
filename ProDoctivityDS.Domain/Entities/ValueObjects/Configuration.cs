using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Application.Dtos.ValueObjects
{
    public class Configuration
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string BearerToken { get; set; } = string.Empty;
        public string CookieSessionId { get; set; } = string.Empty;
        public ProcessingOptions ProcessingOptions { get; set; } = new();
        public AnalysisRuleSet AnalysisRules { get; set; } = new();
    }
}
