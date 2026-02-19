using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Domain.Interfaces
{
    public interface IActivityLogRepository : IBaseRepository<ActivityLogEntry>
    {
        Task AddAsync(ActivityLogEntry log, CancellationToken cancellationToken = default);
        Task<IEnumerable<ActivityLogEntry>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default);
        Task<IEnumerable<ActivityLogEntry>> GetByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
