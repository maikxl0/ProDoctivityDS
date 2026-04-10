using ProDoctivityDS.Application.Dtos.ProDoctivity.Login;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(string username, string password, string sessionId, CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);
        Task<AuthStatusResponse> GetAuthStatusAsync(CancellationToken cancellationToken = default);
    }
}
