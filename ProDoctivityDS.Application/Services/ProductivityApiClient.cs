using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ProDoctivityDS.Application.Services
{
    public class ProductivityApiClient : IProductivityApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IStoredConfigurationRepository _configurationRepository;
        private readonly ILogger<ProductivityApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProductivityApiClient(
            IHttpClientFactory httpClientFactory,
            ILogger<ProductivityApiClient> logger,
            IStoredConfigurationRepository storedConfigurationRepository)
        {
            _configurationRepository = storedConfigurationRepository;
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
                int[] allowedPageSizes = { 15, 30, 100 };
                if (!allowedPageSizes.Contains(pageSize))
                {
                    _logger.LogWarning("El valor de pageSize {PageSize} no es válido. Se usará 100 por defecto.", pageSize);
                    pageSize = 100; // Puedes cambiar a 15 si prefieres
                }

                var client = CreateClient(baseUrl, bearerToken, apiKey, apiSecret, cookie);
                await EnsureValidTokenAsync(cancellationToken);

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
                int[] allowedPageSizes = { 15, 30, 100 };
                if (!allowedPageSizes.Contains(pageSize))
                {
                    _logger.LogWarning("pageSize {PageSize} no válido. Se usará 100.", pageSize);
                    pageSize = 100;
                }
                await EnsureValidTokenAsync(cancellationToken);
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
                await EnsureValidTokenAsync(cancellationToken);
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
        public async Task<DocumentVersionDetailResponse> GetDocumentVersionDetailAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            string versionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureValidTokenAsync(cancellationToken);
                var client = CreateClient(baseUrl, bearerToken);
                var url = $"app/documents/{documentId}/versions/{versionId}";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var detail = JsonSerializer.Deserialize<DocumentVersionDetailResponse>(json, _jsonOptions);
                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalle de versión {VersionId} del documento {DocumentId}", versionId, documentId);
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
                await EnsureValidTokenAsync(cancellationToken);
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
            await EnsureValidTokenAsync(cancellationToken);
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

        public async Task<bool> DeleteDocumentAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken, apiKey, apiSecret, cookie);
                var url = $"app/documents/{documentId}";

                var response = await client.DeleteAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Documento {DocumentId} eliminado exitosamente.", documentId);
                    return true;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Documento {DocumentId} no encontrado para eliminar.", documentId);
                    return false; // O lanza una excepción específica, según prefieras
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error al eliminar documento {DocumentId}. Status: {StatusCode}, Respuesta: {Error}",
                        documentId, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al eliminar documento {DocumentId}", documentId);
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
            object? data = null,
            List<string>? filesName = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureValidTokenAsync(cancellationToken);
                var client = CreateClient(baseUrl, bearerToken); // Solo necesitas bearerToken, los demás ya están en el cliente por defecto
                var url = "app/documents";

                // 1. Convertir PDF a Data URL (sin saltos de línea)
                var base64 = Convert.ToBase64String(pdfContent);
                // En .NET Framework, Convert.ToBase64String puede agregar saltos de línea cada 76 caracteres, los eliminamos
                base64 = base64.Replace("\n", "").Replace("\r", "");
                var dataUrl = $"data:application/pdf;base64,{base64}";

                // 2. Preparar el objeto data (si es null, enviar objeto vacío como en Python)
                var dataObject = data ?? new object();

                // 3. Construir payload exactamente como en Python
                var payload = new Dictionary<string, object>
                {
                    ["documentTypeId"] = documentTypeId,
                    ["contentType"] = "application/pdf",
                    ["data"] = dataObject,
                    ["documents"] = new[] { dataUrl },
                    ["mustUpdateBinaries"] = true,
                    ["parentDocumentVersionId"] = parentVersionId,
                    ["originMethod"] = "imported",
                    ["filesName"] = filesName ?? new List<string>() // Si es null, lista vacía
                };

                // 4. Serializar con camelCase (como espera la API)
                var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                _logger.LogDebug("Payload a enviar: {Payload}", jsonPayload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("PDF subido exitosamente. Respuesta: {Response}", responseBody);
                    return true;
                }
                else
                {
                    _logger.LogError("Error al subir PDF. Status: {StatusCode}, Respuesta: {Response}", response.StatusCode, responseBody);
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
        public async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
        {
            var config = await _configurationRepository.GetActiveConfigurationAsync();
            if (string.IsNullOrEmpty(config.BearerToken) || IsTokenExpired(config.BearerToken))
            {

                var newToken = await LoginAsync(
                    config.ApiBaseUrl,
                    config.Username ?? throw new InvalidOperationException("Username no configurado"),
                    config.Password ?? throw new InvalidOperationException("Password no configurado"),
                    config.ApiKey,
                    config.ApiSecret,
                    config.CookieSessionId,
                    cancellationToken);

                config.BearerToken = newToken;
                await _configurationRepository.UpdateConfigurationAsync(config); // Necesitas implementar este método

            }
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
                    return expirationDate <= DateTime.UtcNow.AddMinutes(5); // margen de 5 minutos
                }
                return true;
            }
            catch
            {
                return true; // Si hay error, asumimos expirado
            }
        }

        public async Task<string> LoginAsync(
            string baseUrl,
            string username,
            string password,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, "", apiKey, apiSecret, cookie); // bearerToken vacío inicialmente
                var url = "users/login";

                var payload = new { username, password };
                var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, _jsonOptions);
                return loginResponse?.Token ?? throw new Exception("No se recibió token en la respuesta");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LoginAsync para usuario {Username}", username);
                throw;
            }
        }
    }
}