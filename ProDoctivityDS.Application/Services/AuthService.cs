using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity.Login;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace ProDoctivityDS.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IStoredConfigurationRepository _configRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IStoredConfigurationRepository configRepository,
            IProductivityApiClient apiClient,
            ILogger<AuthService> logger)
        {
            _configRepository = configRepository;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var config = await _configRepository.GetActiveConfigurationAsync();
                if (config == null)
                {
                    return new LoginResponse { Success = false, Message = "No hay configuración activa en la base de datos." };
                }

                var token = await _apiClient.LoginAsync(
                    config.ApiBaseUrl,
                    username,
                    password,
                    config.ApiKey,
                    config.ApiSecret,
                    config.CookieSessionId,
                    cancellationToken);

                config.BearerToken = token;
                config.Username = username;
                config.Password = password; 

                await _configRepository.UpdateConfigurationAsync(config);

                _logger.LogInformation("Login exitoso para usuario {Username}", username);
                return new LoginResponse { Success = true, Message = "Login exitoso", Token = token };
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Productivity rechazó el login para usuario {Username}", username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Productivity rechazó el acceso (403). Verifica usuario, contraseña y permisos en la API configurada."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login para usuario {Username}", username);
                return new LoginResponse { Success = false, Message = "Error de autenticación: " + ex.Message };
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configRepository.GetActiveConfigurationAsync();
            if (config != null)
            {
                config.BearerToken = null;
                await _configRepository.UpdateConfigurationAsync(config);
                _logger.LogInformation("Sesión cerrada");
            }
        }

        public async Task<AuthStatusResponse> GetAuthStatusAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configRepository.GetActiveConfigurationAsync();
            if (config == null || string.IsNullOrEmpty(config.BearerToken))
                return new AuthStatusResponse { IsAuthenticated = false };

            var isExpired = IsTokenExpired(config.BearerToken);
            return new AuthStatusResponse { IsAuthenticated = !isExpired };
        }

        private bool IsTokenExpired(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                if (expClaim != null && long.TryParse(expClaim, out var expSeconds))
                {
                    var expirationDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                    return expirationDate <= DateTime.UtcNow.AddMinutes(5);
                }
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}
