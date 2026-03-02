using ProDoctivityDS.Application.Dtos.ProDoctivity;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IProductivityApiClient
    {
        /// <summary>
        /// Busca documentos aplicando filtros (paginación, tipo, nombre).
        /// </summary>
        Task<(List<POSTDocumentDto> Documents, int TotalCount)> GetDocumentsAsync(
            string baseUrl,
            string bearerToken,
            List<string>? documentTypeIds,
            string? query,
            int page,
            int pageSize,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default);

        Task<(List<ProductivityDocumentDto> Documents, int TotalCount)> GetAllDocumentsAsync(
            string baseUrl,
            string bearerToken,
            int page,
            int pageSize,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un documento por su ID.
        /// </summary>
        Task<ProductivityDocumentDto> GetDocumentAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la lista de versiones de un documento.
        /// </summary>
        Task<List<ProductivityVersionDto>> GetDocumentVersionsAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarga el PDF de una versión específica.
        /// </summary>
        Task<byte[]> DownloadPdfAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            string versionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sube un nuevo PDF como una nueva versión del documento.
        /// </summary>
        Task<bool> UploadPdfAsync(
            string baseUrl,
            string bearerToken,
            string fileName,
            byte[] pdfContent,
            string documentTypeId,
            string? parentVersionId = null,
            CancellationToken cancellationToken = default);

        Task<List<DocumentTypeDto>> GetDocumentTypesAsync(
            string baseUrl,
            string bearerToken,
            string? apiKey = null,
            string? apiSecret = null,
            string? cookie = null,
            CancellationToken cancellationToken = default);
    }
}
