using Microsoft.Extensions.Logging;
using PdfSharp.Pdf.IO;
using ProDoctivityDS.Application.Interfaces;
using UglyToad.PdfPig;

namespace ProDoctivityDS.Shared.Services
{
    public class PdfManipulatorService : IPdfManipulator
    {
        private readonly ILogger<PdfManipulatorService> _logger;

        public PdfManipulatorService(ILogger<PdfManipulatorService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<byte[]> RemoveFirstPageAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            return await RemovePagesAsync(pdfBytes, new[] { 0 }, cancellationToken);
        }
        public async Task<byte[]> RemovePagesAsync(
            byte[] pdfBytes,
            IEnumerable<int> pageIndices,
            CancellationToken cancellationToken = default)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("El PDF está vacío o es nulo");
                return pdfBytes ?? Array.Empty<byte>();
            }

            var indices = pageIndices?.Distinct().OrderBy(x => x).ToList() ?? new List<int>();
            if (!indices.Any())
                return pdfBytes;

            return await Task.Run(() =>
            {
                try
                {
                    using var inputStream = new MemoryStream(pdfBytes);
                    using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

                    // Eliminar páginas de atrás hacia adelante para no afectar los índices
                    foreach (var idx in indices.OrderByDescending(i => i))
                    {
                        if (idx >= 0 && idx < document.Pages.Count)
                            document.Pages.RemoveAt(idx);
                        else
                            _logger.LogWarning("Índice de página {Index} fuera de rango (total páginas: {Count})", idx + 1, document.Pages.Count);
                    }

                    if (document.Pages.Count == 0)
                    {
                        _logger.LogWarning("El PDF resultante no tiene páginas. Se devuelve el original.");
                        return pdfBytes;
                    }

                    using var outputStream = new MemoryStream();
                    document.Save(outputStream, false);
                    _logger.LogDebug("Páginas eliminadas: {Indices}. Páginas restantes: {Count}",
                        string.Join(", ", indices.Select(i => i + 1)), document.Pages.Count);
                    return outputStream.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar páginas del PDF");
                    return pdfBytes;
                }
            }, cancellationToken);
        }
        public async Task<int> GetPageCountAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(pdfBytes);
                    using var pdf = PdfDocument.Open(stream);
                    return pdf.NumberOfPages;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener número de páginas");
                    return 0;
                }
            }, cancellationToken);
        }
    }
}

