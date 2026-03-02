using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class DocumentTypeService : IDocumentTypeService
    {
        private readonly IStoredConfigurationRepository _configRepo;
        private readonly IProductivityApiClient _apiClient;
        private readonly ILogger<DocumentTypeService> _logger;

        public DocumentTypeService(IStoredConfigurationRepository configRepo, IProductivityApiClient apiClient, ILogger<DocumentTypeService> logger)
        {
            _configRepo = configRepo ?? throw new ArgumentNullException(nameof(configRepo));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException( nameof(logger));
        }

        public async Task<List<DocumentTypeDto>> GetAllDocumentTypes(CancellationToken cancellationToken = default)
        {
            try
            {
                var config = await _configRepo.GetActiveConfigurationAsync();
                var types = await _apiClient.GetDocumentTypesAsync(
                    "https://cloud.prodoctivity.com/svc/api",
                    config.BearerToken,
                    config.ApiKey,
                    config.ApiSecret,
                    config.CookieSessionId,
                    cancellationToken);
                return types;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de documento");
                throw;
            }
        }
    }
}
