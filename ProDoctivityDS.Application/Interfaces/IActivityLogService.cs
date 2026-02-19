using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IActivityLogService : IBaseService<ActivityLogEntry, ActivityLogEntryDto>
    {
    }
}
