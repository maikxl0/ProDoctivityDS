using ProDoctivityDS.Application.Dtos.Response;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface ILogService
    {
        /// <summary>
        /// Registra un nuevo evento en el log.
        /// </summary>
        /// <param name="level">Nivel (INFO, SUCCESS, WARNING, ERROR, DEBUG)</param>
        /// <param name="category">Categoría (ej. "Búsqueda", "Procesamiento", "Análisis")</param>
        /// <param name="message">Mensaje descriptivo</param>
        /// <param name="documentId">ID de documento asociado (opcional)</param>
        Task LogAsync(string level, string category, string message, string? documentId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene los últimos N logs para mostrarlos en el panel.
        /// </summary>
        /// <param name="limit">Número máximo de registros a obtener (por defecto 100)</param>
        Task<IEnumerable<ActivityLogEntryDto>> GetRecentLogsAsync(int limit = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene logs filtrados por nivel.
        /// </summary>
        Task<IEnumerable<ActivityLogEntryDto>> GetLogsByLevelAsync(string level, int limit = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene logs asociados a un documento específico.
        /// </summary>
        Task<IEnumerable<ActivityLogEntryDto>> GetLogsByDocumentIdAsync(string documentId, int limit = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Limpia todos los logs (solo para administración).
        /// </summary>
        Task ClearLogsAsync(CancellationToken cancellationToken = default);
    }
}