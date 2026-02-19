namespace ProDoctivityDS.Domain.Entities.ValueObjects
{
    public class AnalysisRuleSet
    {
        public Criterion Criterion1 { get; set; } = new Criterion
        {
            Text = "SEPARADOR DE DOCUMENTOS",
            IsRegex = false
        };

        public Criterion Criterion2 { get; set; } = new Criterion
        {
            Text = "DOC-001",
            IsRegex = false
        };

        public NormalizationOptions Normalization { get; set; } = new NormalizationOptions();

        // Reservado para futuros cambios (OR es el único por ahora)
        public string EvaluationLogic { get; set; } = "Or";
    }
}
