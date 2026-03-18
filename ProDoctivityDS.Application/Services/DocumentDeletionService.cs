using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace ProDoctivityDS.Application.Services
{
    public class DocumentDeletionService : IDocumentDeletionService
    {
        private readonly IStoredConfigurationRepository _configRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly ILogger<DocumentDeletionService> _logger;

        public DocumentDeletionService(
            IStoredConfigurationRepository configRepository,
            IProductivityApiClient apiClient,
            ILogger<DocumentDeletionService> logger)
        {
            _configRepository = configRepository;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            var config = await _configRepository.GetActiveConfigurationAsync();
            if (config == null)
                throw new InvalidOperationException("No hay configuración activa.");

            // Asegurar token válido (usando lógica de renovación similar a la que ya tienes)
            await EnsureValidTokenAsync(config, cancellationToken);

            return await _apiClient.DeleteDocumentAsync(
                config.ApiBaseUrl,
                config.BearerToken,
                documentId,
                config.ApiKey,
                config.ApiSecret,
                config.CookieSessionId,
                cancellationToken);
        }

        private async Task EnsureValidTokenAsync(StoredConfiguration config, CancellationToken cancellationToken)
        {
            // Reutiliza tu lógica de renovación (por ejemplo, usando un helper o método privado)
            if (string.IsNullOrEmpty(config.BearerToken) || IsTokenExpired(config.BearerToken))
            {
                _logger.LogInformation("Token expirado. Renovando...");
                var newToken = await _apiClient.LoginAsync(
                    config.ApiBaseUrl,
                    config.Username,
                    config.Password,
                    config.ApiKey,
                    config.ApiSecret,
                    config.CookieSessionId,
                    cancellationToken);
                config.BearerToken = newToken;
                await _configRepository.UpdateConfigurationAsync(config);
            }
        }

        private bool IsTokenExpired(string token)
        {
            // Implementación existente (puedes copiarla de otros servicios)
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
