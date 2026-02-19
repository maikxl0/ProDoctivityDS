using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IAnalysisService
    {
        /// <summary>
        /// Obtiene las reglas de análisis actualmente configuradas.
        /// </summary>
        Task<AnalysisRuleSetDto> GetCurrentRulesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda las reglas de análisis.
        /// </summary>
        Task SaveRulesAsync(AnalysisRuleSetDto rulesDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prueba las reglas de análisis en un archivo PDF proporcionado.
        /// </summary>
        Task<AnalysisTestResponseDto> TestPdfAsync(TestAnalysisRequestDto request, CancellationToken cancellationToken = default);
    }
}