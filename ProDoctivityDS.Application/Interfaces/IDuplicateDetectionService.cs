using ProDoctivityDS.Application.Dtos.ProDoctivity;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IDuplicateDetectionService
    {
        Task<DuplicateCheckResponse> CheckDuplicatesByCedulaAsync(string cedula, CancellationToken cancellationToken = default);
    }
}
