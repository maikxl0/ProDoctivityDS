using AutoMapper;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly IStoredConfigurationRepository _configurationRepository;
        private readonly IActivityLogRepository _logRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchService> _logger;

        public SearchService(
            IStoredConfigurationRepository configurationRepository,
            IActivityLogRepository logRepository,
            IProductivityApiClient apiClient,
            IMapper mapper,
            ILogger<SearchService> logger)
        {
            _configurationRepository = configurationRepository;
            _logRepository = logRepository;
            _apiClient = apiClient;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<SearchDocumentsResponseDto> SearchAllAsync(SearchDocumentsRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Obtener configuración activa (contiene credenciales API)
                var config = await _configurationRepository.GetActiveConfigurationAsync();

                // 2. Validar que haya credenciales básicas
                if (string.IsNullOrEmpty(config.ApiBaseUrl) || string.IsNullOrEmpty(config.BearerToken))
                {
                    _logger.LogWarning("Intento de búsqueda sin credenciales API configuradas");
                    return new SearchDocumentsResponseDto
                    {
                        Documents = new List<DocumentDto>(),
                        TotalCount = 0,
                        CurrentPage = request.Page
                    };
                }

                // 3. Llamar al cliente API externo
                var (documents, totalCount) = await _apiClient.GetAllDocumentsAsync(
                    baseUrl: config.ApiBaseUrl,
                    bearerToken: config.BearerToken,
                    page: request.Page,
                    pageSize: request.RowsPerPage,
                    cookie: config.CookieSessionId,
                    apiKey: config.ApiKey,
                    apiSecret: config.ApiSecret,
                    cancellationToken: cancellationToken);

                // 4. Mapear a DTOs de respuesta
                var documentDtos = _mapper.Map<List<DocumentDto>>(documents);

                // 5. Registrar la operación en el log
                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "INFO",
                    Category = "Búsqueda",
                    Message = $"Búsqueda ejecutada: {documentDtos.Count} resultados, página {request.Page}"
                });

                _logger.LogInformation("Búsqueda completada. Total documentos: {TotalCount}, Página: {Page}", totalCount, request.Page);

                // 6. Retornar respuesta
                return new SearchDocumentsResponseDto
                {
                    Documents = documentDtos,
                    TotalCount = totalCount,
                    CurrentPage = request.Page
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar búsqueda de documentos");

                // Registrar error en log
                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "ERROR",
                    Category = "Búsqueda",
                    Message = $"Error en búsqueda: {ex.Message}"
                });

                // Relanzar la excepción para que el controlador la maneje
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<SearchDocumentsPOSTResponseDto> SearchAsync(SearchDocumentsPOSTRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Obtener configuración activa (contiene credenciales API)
                var config = await _configurationRepository.GetActiveConfigurationAsync();

                // 2. Validar que haya credenciales básicas
                if (string.IsNullOrEmpty(config.ApiBaseUrl) || string.IsNullOrEmpty(config.BearerToken))
                {
                    _logger.LogWarning("Intento de búsqueda sin credenciales API configuradas");
                    return new SearchDocumentsPOSTResponseDto
                    {
                        Documents = new List<POSTDocumentDto>(),
                        TotalCount = 0,
                        CurrentPage = request.Page
                    };
                }

                // 3. Llamar al cliente API externo
                var (documents, totalCount) = await _apiClient.GetDocumentsAsync(
                    baseUrl: config.ApiBaseUrl,
                    bearerToken: config.BearerToken,
                    documentTypeIds: request.DocumentTypeId,
                    query: request.Name,
                    page: request.Page,
                    pageSize: request.RowsPerPage,
                    cookie: config.CookieSessionId,
                    apiKey: config.ApiKey,
                    apiSecret: config.ApiSecret,
                    cancellationToken: cancellationToken);

                // 4. Mapear a DTOs de respuesta
                var documentDtos = _mapper.Map<List<POSTDocumentDto>>(documents);

                // 5. Registrar la operación en el log
                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "INFO",
                    Category = "Búsqueda",
                    Message = $"Búsqueda ejecutada: {documentDtos.Count} resultados, página {request.Page}"
                });

                _logger.LogInformation("Búsqueda completada. Total documentos: {TotalCount}, Página: {Page}", totalCount, request.Page);

                // 6. Retornar respuesta
                return new SearchDocumentsPOSTResponseDto
                {
                    Documents = documentDtos,
                    TotalCount = totalCount,
                    CurrentPage = request.Page
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar búsqueda de documentos");

                // Registrar error en log
                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "ERROR",
                    Category = "Búsqueda",
                    Message = $"Error en búsqueda: {ex.Message}"
                });

                // Relanzar la excepción para que el controlador la maneje
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DocumentDto> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Obtener configuración activa
                var config = await _configurationRepository.GetActiveConfigurationAsync();

                // 2. Llamar a la API para obtener el documento
                var document = await _apiClient.GetDocumentAsync(
                    config.ApiBaseUrl,
                    config.BearerToken,
                    documentId,
                    cancellationToken);

                // 3. Mapear a DTO
                var documentDto = _mapper.Map<DocumentDto>(document);

                // 4. Registrar en log
                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "INFO",
                    Category = "Documento",
                    Message = $"Detalles del documento obtenidos: {documentId}"
                });

                return documentDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documento {DocumentId}", documentId);
                throw;
            }
        }
    }
}