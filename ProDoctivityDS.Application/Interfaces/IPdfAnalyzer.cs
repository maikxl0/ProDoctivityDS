using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IPdfAnalyzer
    {
        /// <summary>
        /// Extrae el texto completo de la primera página de un PDF.
        /// </summary>
        Task<string> ExtractFirstPageTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aplica las reglas de normalización a un texto.
        /// </summary>
        string NormalizeText(string text, NormalizationOptions options);

        /// <summary>
        /// Evalúa si el texto de la primera página cumple los criterios (OR).
        /// </summary>
        bool ShouldRemoveFirstPage(string firstPageText, AnalysisRuleSet rules);

        /// <summary>
        /// Analiza un PDF completo y retorna un diagnóstico detallado.
        /// </summary>
        

        // Analiza una página específica (pageIndex 0-based)
        Task<PageAnalysisResult> AnalyzePageAsync(
            byte[] pdfBytes,
            int pageIndex,
            AnalysisRuleSet rules,
            CancellationToken cancellationToken = default);

        // Mantenemos el método original para compatibilidad (opcional)
        Task<AnalysisResultDto> AnalyzePdfAsync(
            byte[] pdfBytes,
            AnalysisRuleSet rules,
            CancellationToken cancellationToken = default);
    }
}
