using ProDoctivityDS.Application.Dtos.Request;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IProcessingService
    {
        /// <summary>
        /// Procesa una lista de documentos seleccionados.
        /// El progreso se actualiza en ProcessingProgressStore usando el sessionId.
        /// </summary>
        Task ProcessDocumentsAsync(ProcessRequestDto request, string sessionId, CancellationToken cancellationToken = default);
    }
}