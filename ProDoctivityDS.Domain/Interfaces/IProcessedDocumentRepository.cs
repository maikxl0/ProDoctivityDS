using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;

namespace Domain.Interfaces.Repositories
{
    public interface IProcessedDocumentRepository : IBaseRepository<ProcessedDocument>
    {
        /// <summary>
        /// Obtiene los documentos procesados en un rango de fechas.
        /// </summary>
        Task<IEnumerable<ProcessedDocument>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }
}