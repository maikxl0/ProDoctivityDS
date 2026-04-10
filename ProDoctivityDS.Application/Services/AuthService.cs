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
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IStoredConfigurationRepository configRepository,
            IProductivityApiClient apiClient,
            ICurrentUserService currentUserService,
            ILogger<AuthService> logger)
        {
            _configRepository = configRepository;
            _apiClient = apiClient;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(string username, string password, string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Usar configuración por defecto para credenciales de API
                var defaultConfig = await _configRepository.GetDefaultConfigurationAsync();
                if (defaultConfig == null || string.IsNullOrEmpty(defaultConfig.ApiBaseUrl))
                {
                    return new LoginResponse { Success = false, Message = "No hay configuración de API activa. Configure la API primero." };
                }

                var token = await _apiClient.LoginAsync(
                    defaultConfig.ApiBaseUrl,
                    username,
                    password,
                    defaultConfig.ApiKey,
                    defaultConfig.ApiSecret,
                    defaultConfig.CookieSessionId,
                    cancellationToken);

                // Cargar o crear configuración per-user
                var userConfig = await _configRepository.GetConfigurationForUserAsync(username);
                userConfig.BearerToken = token;
                userConfig.Username = username;
                userConfig.Password = password;
                // Asegurar que las credenciales de API estén actualizadas
                userConfig.ApiBaseUrl = defaultConfig.ApiBaseUrl;
                userConfig.ApiKey = defaultConfig.ApiKey;
                userConfig.ApiSecret = defaultConfig.ApiSecret;
                userConfig.CookieSessionId = defaultConfig.CookieSessionId;

                await _configRepository.UpdateConfigurationForUserAsync(username, userConfig);

                // Registrar mapeo sesión → usuario
                _currentUserService.SetUsername(sessionId, username);

                _logger.LogInformation("Login exitoso para usuario {Username}, sesión {SessionId}", username, sessionId);
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
