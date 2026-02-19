using Microsoft.EntityFrameworkCore;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using ProDoctivityDS.Persistence.Context;

namespace ProDoctivityDS.Persistence.Repositories
{
    public class ActivityLogRepository : BaseRepository<ActivityLogEntry>, IActivityLogRepository
    {
        private readonly ProDoctivityDSDbContext _context;

        public ActivityLogRepository(ProDoctivityDSDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddAsync(ActivityLogEntry log, CancellationToken cancellationToken = default)
        {
            var entity = log;
            await _context.ActivityLogs.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<ActivityLogEntry>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            var entities = await _context.ActivityLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync(cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<ActivityLogEntry>> GetByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default)
        {
            var entities = await _context.ActivityLogs
                .Where(l => l.DocumentId == documentId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync(cancellationToken);
            return entities;
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}