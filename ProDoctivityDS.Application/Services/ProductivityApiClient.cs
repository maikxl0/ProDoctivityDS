using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ProDoctivityDS.Application.Services
{
    public class ProductivityApiClient : IProductivityApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductivityApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProductivityApiClient(
            IHttpClientFactory httpClientFactory,
            ILogger<ProductivityApiClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        private HttpClient CreateClient(string baseUrl, string bearerToken, string? apiKey = null, string? apiSecret = null, string? cookie = null)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Clear();

            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            if (!string.IsNullOrEmpty(apiSecret))
                client.DefaultRequestHeaders.Add("x-api-secret", apiSecret);
            if (!string.IsNullOrEmpty(bearerToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            if (!string.IsNullOrEmpty(cookie))
                client.DefaultRequestHeaders.Add("Cookie", cookie);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<(List<ProductivityDocumentDto> Documents, int TotalCount)> GetAllDocumentsAsync(
            string baseUrl,
            string bearerToken,
            int page,
            int pageSize,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar que pageSize sea 15, 30 o 100
                int[] allowedPageSizes = { 15, 30, 100 };
                if (!allowedPageSizes.Contains(pageSize))
                {
                    _logger.LogWarning("El valor de pageSize {PageSize} no es válido. Se usará 100 por defecto.", pageSize);
                    pageSize = 100; // Puedes cambiar a 15 si prefieres
                }

                var client = CreateClient(baseUrl, bearerToken, apiKey, apiSecret, cookie);

                string url = $"app/documents?dateStart=0&pageNumber={page}&rowsPerPage={pageSize}&sortField=updatedAt&sortDirection=DESC";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await client.SendAsync(request, cancellationToken);

                // 🔍 LOG: Código de estado y contenido de error si lo hay
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error {StatusCode}. Respuesta: {Error}", response.StatusCode, errorContent);
                    response.EnsureSuccessStatusCode(); // Lanza excepción con el detalle
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResponse = JsonSerializer.Deserialize<ProductivitySearchResponseDto>(json, _jsonOptions);

                var documents = searchResponse?.Documents?.ToList() ?? new List<ProductivityDocumentDto>();
                int totalCount = searchResponse?.Total ?? documents.Count;

                return (documents, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAllDocumentsAsync");
                throw;
            }
        }


        // ---------- GetDocumentsAsync ----------
        public async Task<(List<POSTDocumentDto> Documents, int TotalCount)> GetDocumentsAsync(
            string baseUrl,
            string bearerToken,
            List<string>? documentTypeIds,
            string? query,
            int page,
            int pageSize,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar pageSize (15, 30, 100)
                int[] allowedPageSizes = { 15, 30, 100 };
                if (!allowedPageSizes.Contains(pageSize))
                {
                    _logger.LogWarning("pageSize {PageSize} no válido. Se usará 100.", pageSize);
                    pageSize = 100;
                }

                var client = CreateClient(baseUrl, bearerToken, apiKey, apiSecret, cookie);

                bool hasQuery = !string.IsNullOrWhiteSpace(query);
                bool hasTypeFilters = documentTypeIds != null && documentTypeIds.Any();
                HttpRequestMessage request;

                // Construir URL base (siempre con paginación)
                string url = $"app/search?pageNumber={page}&rowsPerPage={pageSize}";

                // Si hay query, agregarlo a la URL (como en el ejemplo funcional)
                if (hasQuery)
                {
                    url += $"&query={Uri.EscapeDataString(query!)}";
                }


                if (hasTypeFilters)
                {
                    // Si hay filtros de tipo de documento, enviar payload con los nombres en minúsculas
                    var payload = new
                    {
                        fields = new List<string>(),
                        documentTypeIds = documentTypeIds,
                        includeApproximateResults = false
                    };

                    string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Asegura minúsculas
                    });

                    request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    // Solo query (sin documentTypeIds): cuerpo vacío (como en el ejemplo funcional)
                    request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent("", Encoding.UTF8, "application/json")
                    };
                }

                // Enviar petición
                var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error {StatusCode}. Respuesta: {Error}", response.StatusCode, errorContent);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("Respuesta completa: {Json}", json); // Para depurar

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Permite coincidir aunque haya diferencias de mayúsculas/minúsculas
                };
                var searchResponse = JsonSerializer.Deserialize<ProductivitySearchPOSTResponseDto>(json, options);

                var documents = searchResponse?.Results ?? new List<POSTDocumentDto>();
                int totalCount = searchResponse?.TotalRowCount ?? documents.Count;

                return (documents, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetDocumentsAsync");
                throw;
            }
        }

        // ---------- GetDocumentAsync ----------
        public async Task<ProductivityDocumentDto> GetDocumentAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = $"documents/{documentId}";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var container = JsonSerializer.Deserialize<ProductivityDocumentResponse>(json, _jsonOptions);
                var document = container?.Document ?? throw new Exception("No se encontró la propiedad 'document' en la respuesta");

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documento {DocumentId}", documentId);
                throw;
            }
        }

        // ---------- GetDocumentVersionsAsync ----------
        
        public async Task<List<ProductivityVersionDto>> GetDocumentVersionsAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = $"app/documents/{documentId}/versions-list";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var versions = JsonSerializer.Deserialize<DocumentVersionsResponse>(json, _jsonOptions);
                return versions?.DocumentVersions ?? new List<ProductivityVersionDto>();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener versiones del documento {DocumentId}", documentId);
                throw;
            }
        }

        // ---------- GetDocumentTypesAsync ----------
        public async Task<List<DocumentTypeDto>> GetDocumentTypesAsync(
            string baseUrl,
            string bearerToken,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default)
        {
            var client = CreateClient(baseUrl, bearerToken, apiKey, apiSecret, cookie);
            var url = "ecm/document-types";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await client.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var container = JsonSerializer.Deserialize<DocumentTypeListResponse>(json, _jsonOptions);
            return container?.DocumentTypes ?? new List<DocumentTypeDto>();
        }

        // ---------- DownloadPdfAsync ----------
        public async Task<byte[]> DownloadPdfAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            string versionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = $"app/documents/{documentId}/versions/{versionId}"; // Asumiendo endpoint directo
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var detail = JsonSerializer.Deserialize<DocumentVersionDetailResponse>(json, _jsonOptions);

                /// Extraer el primer data URL que sea PDF
                var pdfDataUrl = detail?.Document?.Binaries?
                    .FirstOrDefault(b => b.Contains("application/pdf") || b.Contains("application/octet-stream"));

                if (string.IsNullOrEmpty(pdfDataUrl))
                {
                    _logger.LogWarning("No se encontró PDF en los binarios de la versión {VersionId}", versionId);
                    return Array.Empty<byte>();
                }

                return DataUrlToBytes(pdfDataUrl);
            
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar PDF de versión {VersionId}", versionId);
                throw;
            }
        }


        // ---------- UploadPdfAsync ----------
        public async Task<bool> UploadPdfAsync(
            string baseUrl,
            string bearerToken,
            string fileName,
            byte[] pdfContent,
            string documentTypeId,
            string? parentVersionId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = "app/documents";

                // Convertir PDF a Data URL
                var dataUrl = BytesToDataUrl(pdfContent, "application/pdf");

                var payload = new ProductivityUploadRequestDto
                {
                    DocumentTypeId = documentTypeId,
                    ContentType = "application/pdf",
                    Data = new { }, // Si se requieren metadatos adicionales, se pueden agregar
                    Documents = new List<string> { dataUrl },
                    MustUpdateBinaries = true,
                    ParentDocumentVersionId = parentVersionId,
                    FilesName = new List<string> { fileName },
                    OriginMethod = "imported"
                };

                var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("PDF subido exitosamente. Nombre: {FileName}", fileName);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error al subir PDF. Status: {StatusCode}, Respuesta: {Error}", response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir PDF {FileName}", fileName);
                return false;
            }
        }

        private byte[] DataUrlToBytes(string dataUrl)
        {
            var base64Data = dataUrl.Substring(dataUrl.IndexOf(",") + 1);
            return Convert.FromBase64String(base64Data);
        }

        private string BytesToDataUrl(byte[] bytes, string mimeType)
        {
            var base64 = Convert.ToBase64String(bytes);
            return $"data:{mimeType};base64,{base64}";
        }

        
    }
}