namespace ProDoctivityDS.Domain.Entities.ValueObjects
{
    public class AnalysisRuleSet
    {
        public string KeywordSeparador { get; set; } = "SEPARADOR DE DOCUMENTOS";
        public string KeywordCodigo { get; set; } = "DOC-001";
        public NormalizationOptions Normalization { get; set; } = new NormalizationOptions();
        public int SearchCharacterLimit { get; set; } = 25;
    }
}
