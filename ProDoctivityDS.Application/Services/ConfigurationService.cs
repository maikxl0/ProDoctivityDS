using AutoMapper;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace ProDoctivityDS.Application.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IStoredConfigurationRepository _configurationRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly IMapper _mapper;
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(
            IStoredConfigurationRepository configurationRepository,
            IProductivityApiClient apiClient,
            IMapper mapper,
            ILogger<ConfigurationService> logger)
        {
            _configurationRepository = configurationRepository;
            _apiClient = apiClient;
            _mapper = mapper;
            _logger = logger;
        }
        

        /// <inheritdoc />
        public async Task<ConfigurationDto> GetConfigurationDtoAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configurationRepository.GetActiveConfigurationAsync();

            var dto = _mapper.Map<ConfigurationDto>(config);

            // Ocultar credenciales sensibles
            dto.ApiKey = string.IsNullOrEmpty(config.ApiKey) ? "" : "●●●●●●●●";
            dto.ApiSecret = string.IsNullOrEmpty(config.ApiSecret) ? "" : "●●●●●●●●";
            dto.BearerToken = string.IsNullOrEmpty(config.BearerToken) ? "" : "●●●●●●●●";
            dto.CookieSessionId = string.IsNullOrEmpty(config.CookieSessionId) ? "" : "●●●●●●●●";
            dto.BaseUrl = config.ApiBaseUrl;
            return dto;
        }

        /// <inheritdoc />
        public async Task SaveConfigurationAsync(SaveConfigurationRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Cargar configuración existente para preservar campos de usuario
                var existing = await _configurationRepository.GetActiveConfigurationAsync();

                // Mapear DTO a entidad de dominio
                var config = _mapper.Map<StoredConfiguration>(request);

                // Preservar campos per-user que no vienen en el DTO de configuración
                config.Username = existing.Username;
                config.Password = existing.Password;
                config.BearerToken = existing.BearerToken;

                // Actualizar en repositorio
                await _configurationRepository.UpdateConfigurationAsync(config);

                _logger.LogInformation("Configuración guardada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la configuración");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> TestConnectionAsync(ApiCredentialsDto credentials, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Probando conexión a la API de Productivity");

                var (documents, totalCount) = await _apiClient.GetDocumentsAsync(
                    baseUrl: credentials.BaseUrl,
                    bearerToken: credentials.BearerToken,
                    documentTypeIds: null,
                    query: null,
                    page: 0,
                    pageSize: 15,
                    apiKey: credentials.ApiKey,
                    apiSecret: credentials.ApiSecret,
                    cookie: credentials.CookieSessionId,
                    cancellationToken: cancellationToken
        );

                var success = documents != null;

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al probar la conexión");
                
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<ConfigurationDto> ExportConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configurationRepository.GetActiveConfigurationAsync();
            _logger.LogDebug("Config recuperada: BaseUrl={BaseUrl}, BearerToken length={BearerLen}, ApiKey length={ApiKeyLen}, ApiSecret length={ApiSecretLen}, Cookie length={CookieLen}",
                              config?.ApiBaseUrl,
                              config?.BearerToken?.Length,
                              config?.ApiKey?.Length,
                              config?.ApiSecret?.Length,
                              config?.CookieSessionId?.Length);

            // Mapear a DTO (incluye credenciales en texto plano porque el repositorio ya las descifró)
            var dto = _mapper.Map<ConfigurationDto>(config);
            dto.BaseUrl = config.ApiBaseUrl;

            _logger.LogInformation("Configuración exportada");

            return dto;
        }

        /// <inheritdoc />
        public async Task ImportConfigurationAsync(ConfigurationDto importDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Mapear DTO a entidad de dominio
                var config = _mapper.Map<StoredConfiguration>(importDto);

                // Guardar configuración (el repositorio cifrará las credenciales)
                await _configurationRepository.UpdateConfigurationAsync(config);

                _logger.LogInformation("Configuración importada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar configuración");
                throw;
            }
        }
    }
}
