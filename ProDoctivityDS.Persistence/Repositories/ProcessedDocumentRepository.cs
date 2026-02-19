using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Persistence.Context;
using ProDoctivityDS.Persistence.Repositories;

namespace Infrastructure.Repositories
{
    public class ProcessedDocumentRepository : BaseRepository<ProcessedDocument>, IProcessedDocumentRepository
    {
        private readonly ProDoctivityDSDbContext _context;

        public ProcessedDocumentRepository(ProDoctivityDSDbContext context) : base(context) 
        {
            _context = context;
        }

        public async Task<IEnumerable<ProcessedDocument>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var entities = await _context.ProcessedDocuments
                .Where(p => p.ProcessingDate >= from && p.ProcessingDate <= to)
                .OrderByDescending(p => p.ProcessingDate)
                .ToListAsync(cancellationToken);
            return entities;
        }
    }
}