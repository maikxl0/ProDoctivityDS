using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Interfaces;
using System.Security.Cryptography;

namespace ProDoctivityDS.Application.Services
{
    public class DuplicateDetectionService : IDuplicateDetectionService
    {
        private readonly IStoredConfigurationRepository _configRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly ILogger<DuplicateDetectionService> _logger;

        public DuplicateDetectionService(
            IStoredConfigurationRepository configRepository,
            IProductivityApiClient apiClient,
            ILogger<DuplicateDetectionService> logger)
        {
            _configRepository = configRepository;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<DuplicateCheckResponse> CheckDuplicatesByCedulaAsync(string cedula, CancellationToken cancellationToken = default)
        {
            var response = new DuplicateCheckResponse();

            // 1. Obtener configuración activa (incluye token)
            var config = await _configRepository.GetActiveConfigurationAsync();
            if (config == null || string.IsNullOrEmpty(config.BearerToken))
                throw new InvalidOperationException("No hay configuración activa o token válido.");

            // 2. Buscar documentos que contengan la cédula (en nombre o contenido)
            var allDocuments = new List<POSTDocumentDto>();
            int page = 0;
            int pageSize = 100; // máximo permitido
            int totalCount;

            do
            {
                var (documents, total) = await _apiClient.GetDocumentsAsync(
                    config.ApiBaseUrl,
                    config.BearerToken,
                    null, // documentTypeIds
                    cedula, // query
                    page,
                    pageSize,
                    config.ApiKey,
                    config.ApiSecret,
                    config.CookieSessionId,
                    cancellationToken);

                allDocuments.AddRange(documents);
                totalCount = total;
                page++;
            } while (allDocuments.Count < totalCount && !cancellationToken.IsCancellationRequested);

            _logger.LogInformation("Se encontraron {Count} documentos para la cédula {Cedula}", allDocuments.Count, cedula);
            response.TotalDocuments = allDocuments.Count;

            // 3. Obtener detalles de cada documento (para tamaño y hash)
            var documentDetails = new List<(POSTDocumentDto doc, long fileSize, string fileHash)>();
            foreach (var doc in allDocuments)
            {
                try
                {
                    var detail = await _apiClient.GetDocumentVersionDetailAsync(
                        config.ApiBaseUrl,
                        config.BearerToken,
                        doc.DocumentId,
                        doc.DocumentVersionId,
                        cancellationToken);

                    // Extraer el PDF y calcular tamaño y hash
                    var pdfDataUrl = detail?.Document?.Binaries?.FirstOrDefault(b => b.Contains("application/pdf"));
                    if (!string.IsNullOrEmpty(pdfDataUrl))
                    {
                        var pdfBytes = DataUrlToBytes(pdfDataUrl);
                        long size = pdfBytes.Length;
                        string hash = ComputeHash(pdfBytes);

                        documentDetails.Add((doc, size, hash));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener detalle del documento {DocumentId}", doc.DocumentId);
                }
            }

            // 4. Agrupar por clave de duplicado: TipoDocumento + Tamaño + Hash (o solo Tipo + Hash)
            var groups = documentDetails
                .GroupBy(x => $"{x.doc.DocumentTypeId}_{x.fileHash}") // clave única
                .Where(g => g.Count() > 1) // solo grupos con duplicados
                .Select(g => new DuplicateGroupDto
                {
                    GroupKey = g.Key,
                    Reason = "Mismo tipo de documento y mismo contenido (hash)",
                    Documents = g.Select(item => new DuplicateDocumentDto
                    {
                        DocumentId = item.doc.DocumentId,
                        Name = item.doc.Name,
                        DocumentTypeId = item.doc.DocumentTypeId,
                        DocumentTypeName = item.doc.DocumentTypeName,
                        CreatedAt = item.doc.CreatedAt != null ? DateTimeOffset.FromUnixTimeMilliseconds((long)item.doc.CreatedAt).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss"): null,
                        FileSize = item.fileSize,
                        FileHash = item.fileHash,
                        
                        GroupKey = g.Key
                    }).ToList()
                }).ToList();

            response.Groups = groups;

            return response;
        }

        private string ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        private byte[] DataUrlToBytes(string dataUrl)
        {
            var base64Data = dataUrl.Substring(dataUrl.IndexOf(",") + 1);
            return Convert.FromBase64String(base64Data);
        }
    }
}