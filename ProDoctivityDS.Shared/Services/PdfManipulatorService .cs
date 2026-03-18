using iText.Forms;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Shared.Services
{
    public class PdfManipulatorService : IPdfManipulator
    {
        private readonly ILogger<PdfManipulatorService> _logger;

        public PdfManipulatorService(ILogger<PdfManipulatorService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> RemovePagesAsync(byte[] pdfBytes, IEnumerable<int> pageIndices, CancellationToken cancellationToken = default)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                return pdfBytes ?? Array.Empty<byte>();

            var indices = pageIndices?.Distinct().OrderBy(x => x).ToList() ?? new List<int>();
            if (!indices.Any())
                return pdfBytes;

            return await Task.Run(() =>
            {
                try
                {
                    using var srcStream = new MemoryStream(pdfBytes);
                    using var reader = new PdfReader(srcStream);
                    using var destStream = new MemoryStream();

                    // Desactivar modo inteligente para evitar problemas de serialización
                    var writerProps = new WriterProperties().SetCompressionLevel(CompressionConstants.NO_COMPRESSION)
                                                            .SetFullCompressionMode(false); ;
                    using var writer = new PdfWriter(destStream, writerProps);

                    using var srcPdfDoc = new PdfDocument(reader);
                    using var destPdfDoc = new PdfDocument(writer);

                    int totalPages = srcPdfDoc.GetNumberOfPages();
                    var pagesToKeep = Enumerable.Range(1, totalPages)
                                                .Where(p => !indices.Contains(p - 1))
                                                .ToList();

                    if (!pagesToKeep.Any())
                    {
                        _logger.LogWarning("El PDF resultante no tiene páginas. Se devuelve el original.");
                        return pdfBytes;
                    }

                    srcPdfDoc.CopyPagesTo(pagesToKeep, destPdfDoc);

                    // Copiar metadatos básicos (DocumentInfo)
                    var srcInfo = srcPdfDoc.GetDocumentInfo();
                    var destInfo = destPdfDoc.GetDocumentInfo();
                    if (srcInfo != null)
                    {
                        if (!string.IsNullOrEmpty(srcInfo.GetTitle())) destInfo.SetTitle(srcInfo.GetTitle());
                        if (!string.IsNullOrEmpty(srcInfo.GetAuthor())) destInfo.SetAuthor(srcInfo.GetAuthor());
                        if (!string.IsNullOrEmpty(srcInfo.GetSubject())) destInfo.SetSubject(srcInfo.GetSubject());
                        if (!string.IsNullOrEmpty(srcInfo.GetKeywords())) destInfo.SetKeywords(srcInfo.GetKeywords());
                        if (!string.IsNullOrEmpty(srcInfo.GetCreator())) destInfo.SetCreator(srcInfo.GetCreator());
                        if (!string.IsNullOrEmpty(srcInfo.GetProducer())) destInfo.SetProducer(srcInfo.GetProducer());
                    }

                    // Copiar solo propiedades esenciales del formulario
                    CopyAcroFormSafe(srcPdfDoc, destPdfDoc);

                    destPdfDoc.Close();

                    // Validar el PDF generado antes de devolverlo
                    byte[] result = destStream.ToArray();
                    if (!IsPdfValid(result))
                    {
                        _logger.LogError("El PDF generado está corrupto. Se devuelve el original.");
                        return pdfBytes;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar páginas del PDF con iText");
                    return pdfBytes;
                }
            }, cancellationToken);
        }

        private void CopyAcroFormSafe(PdfDocument srcDoc, PdfDocument destDoc)
        {
            try
            {
                var srcForm = PdfAcroForm.GetAcroForm(srcDoc, false);
                if (srcForm == null)
                    return;

                var destForm = PdfAcroForm.GetAcroForm(destDoc, true);

                // Solo copiamos la propiedad NeedAppearances (es la más relevante)
                var needAppearances = srcForm.GetNeedAppearances();
                if (needAppearances != null)
                {
                    destForm.SetNeedAppearances(needAppearances.GetValue());
                }

                // Si los campos se copiaron con las páginas, no necesitamos más.
                // Si algún campo global falta, se puede intentar copiar, pero es arriesgado.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo copiar el AcroForm. Los campos podrían perderse.");
            }
        }

        private bool IsPdfValid(byte[] pdfBytes)
        {
            try
            {
                using var stream = new MemoryStream(pdfBytes);
                using var reader = new PdfReader(stream);
                using var dummy = new PdfDocument(reader);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetPageCountAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                using var stream = new MemoryStream(pdfBytes);
                using var reader = new PdfReader(stream);
                using var pdfDoc = new PdfDocument(reader);
                return pdfDoc.GetNumberOfPages();
            }, cancellationToken);
        }

        public async Task<byte[]> RemoveFirstPageAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            return await RemovePagesAsync(pdfBytes, new[] { 0 }, cancellationToken);
        }

        #region Métodos auxiliares privados

        /// <summary>
        /// Copia el AcroForm (campos de formulario) del documento origen al destino.
        /// </summary>
        private void CopyAcroForm(PdfDocument srcDoc, PdfDocument destDoc)
        {
            try
            {
                var srcForm = PdfAcroForm.GetAcroForm(srcDoc, false);
                if (srcForm == null)
                    return;

                var destForm = PdfAcroForm.GetAcroForm(destDoc, true);

                // Copiar el diccionario completo del formulario (preserva todas las propiedades)
                var srcFormDict = srcForm.GetPdfObject();
                var destFormDict = destForm.GetPdfObject();
                if (srcFormDict != null && destFormDict != null)
                {
                    foreach (var key in srcFormDict.KeySet())
                    {
                        // Evitar sobrescribir el tipo (si ya está definido) o claves internas problemáticas
                        if (!key.Equals(PdfName.Type))
                        {
                            destFormDict.Put(key, srcFormDict.Get(key).CopyTo(destDoc));
                        }
                    }
                }

                // Opcional: Si hay campos que necesitan regenerar apariencias
                var needAppearances = srcForm.GetNeedAppearances();
                if (needAppearances != null)
                {
                    destForm.SetNeedAppearances(needAppearances.GetValue());
                }

                _logger.LogDebug("AcroForm copiado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo copiar el AcroForm completo. Los campos podrían perderse.");
            }
        }

        /// <summary>
        /// Copia los metadatos XMP si están presentes. 
        /// Esta funcionalidad requiere el paquete itext7.xmp.
        /// Si no está disponible, se omite silenciosamente.
        /// </summary>
        private void CopyXmpMetadata(PdfDocument srcDoc, PdfDocument destDoc)
        {
            try
            {
                // Intentar obtener el flujo XMP
                var srcXmp = srcDoc.GetXmpMetadata();
                if (srcXmp != null)
                {
                    destDoc.SetXmpMetadata(srcXmp);
                    _logger.LogDebug("Metadatos XMP copiados.");
                }
            }
            catch (TypeLoadException)
            {
                // La dependencia XMP no está instalada
                _logger.LogDebug("La funcionalidad XMP no está disponible (falta itext7.xmp). Se omiten metadatos XMP.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al copiar metadatos XMP (se omiten).");
            }
        }

        #endregion
    }
}