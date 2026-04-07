using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly IStoredConfigurationRepository _configurationRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly IPdfAnalyzer _pdfAnalyzer;
        private readonly IPdfManipulator _pdfManipulator;
        private readonly IFileStorageService _fileStorage;
        private readonly IProcessingProgressStore _progressStore;
        private readonly ILogger<ProcessingService> _logger;

        public ProcessingService(
            IStoredConfigurationRepository configurationRepository,
            IProductivityApiClient apiClient,
            IPdfAnalyzer pdfAnalyzer,
            IPdfManipulator pdfManipulator,
            IFileStorageService fileStorage,
            IProcessingProgressStore progressStore,
            ILogger<ProcessingService> logger)
        {
            _configurationRepository = configurationRepository;
            _apiClient = apiClient;
            _pdfAnalyzer = pdfAnalyzer;
            _pdfManipulator = pdfManipulator;
            _fileStorage = fileStorage;
            _progressStore = progressStore;
            _logger = logger;
        }

        public async Task ProcessDocumentsAsync(
            ProcessRequestDto request,
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            // 1. Obtener configuración activa
            StoredConfiguration config = await _configurationRepository.GetActiveConfigurationAsync();
            ProcessingOptions processingOptions = config.ProcessingOptions ?? new ProcessingOptions();
            AnalysisRuleSet analysisRules = config.AnalysisRules ?? new AnalysisRuleSet();

            bool updateApi = request.UpdateApi ?? processingOptions.UpdateApi;
            bool saveOriginals = request.SaveOriginals ?? processingOptions.SaveOriginalFiles;

            int total = request.DocumentIds.Count;
            int processed = 0, updated = 0, pagesRemoved = 0, errors = 0, skipped = 0;

            // Inicializar progreso
            _progressStore.UpdateProgress(sessionId, new ProcessProgressDto
            {
                Total = total,
                Processed = 0,
                Updated = 0,
                PagesRemoved = 0,
                Errors = 0,
                Skipped = 0,
                CurrentDocumentName = "Iniciando...",
                Status = "Iniciando"
            });

            await LogAsync("INFO", "Procesamiento", $"Iniciando procesamiento de {total} documento(s) (sesión: {sessionId})");

            for (int i = 0; i < request.DocumentIds.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await LogAsync("WARNING", "Procesamiento", $"Procesamiento cancelado por el usuario (sesión: {sessionId})");
                    break;
                }

                var documentId = request.DocumentIds[i];
                string documentName = string.Empty;

                try
                {
                    // Obtener información del documento
                    ProductivityDocumentDto document = await _apiClient.GetDocumentAsync(
                        config.ApiBaseUrl, config.BearerToken, documentId, cancellationToken);
                    documentName = document.Name;

                    // Actualizar progreso
                    _progressStore.UpdateProgress(sessionId, new ProcessProgressDto
                    {
                        Total = total,
                        Processed = processed,
                        Updated = updated,
                        PagesRemoved = pagesRemoved,
                        Errors = errors,
                        Skipped = skipped,
                        CurrentDocumentId = documentId,
                        CurrentDocumentName = documentName,
                        Status = $"Procesando {i + 1}/{total}: {documentName}"
                    });

                    // Obtener la última versión del documento
                    string versionId = await GetLatestVersionIdAsync(document, documentId, config, cancellationToken);
                    var versionDetail = await _apiClient.GetDocumentVersionDetailAsync(
                        config.ApiBaseUrl, config.BearerToken, documentId, versionId, cancellationToken);

                    var pdfDataUrl = versionDetail?.Document?.Binaries?
                        .FirstOrDefault(b => b.Contains("application/pdf") || b.Contains("application/octet-stream"));
                    if (string.IsNullOrEmpty(pdfDataUrl))
                        throw new Exception("No se encontró PDF en los binarios de la versión");

                    byte[] pdfBytes = DataUrlToBytes(pdfDataUrl);
                    var originalData = versionDetail.Document.Data;
                    var originalFilesName = versionDetail.Document.FilesName;

                    // Guardar original si está activado
                    if (saveOriginals)
                    {
                        await _fileStorage.SaveFileAsync(pdfBytes, $"{documentId}_original", "originals", cancellationToken);
                    }

                    // Determinar páginas a eliminar
                    List<int> pagesToRemove = await DeterminePagesToRemoveAsync(
                        pdfBytes, processingOptions, analysisRules, cancellationToken);

                    // Remover páginas si es necesario
                    byte[] processedPdf = pdfBytes;
                    bool anyPageRemoved = false;
                    if (pagesToRemove.Any())
                    {
                        processedPdf = await _pdfManipulator.RemovePagesAsync(pdfBytes, pagesToRemove.Distinct().OrderBy(x => x), cancellationToken);
                        anyPageRemoved = true;
                        pagesRemoved += pagesToRemove.Count;
                        _logger.LogInformation("Documento {DocumentId}: eliminadas {Count} páginas: [{Pages}]",
                            documentId, pagesToRemove.Count, string.Join(",", pagesToRemove.Select(p => p + 1)));
                    }

                    // Actualizar en API si corresponde
                    bool apiUpdated = false;
                    if (updateApi && anyPageRemoved)
                    {
                        apiUpdated = await _apiClient.UploadPdfAsync(
                            config.ApiBaseUrl, config.BearerToken, documentName, processedPdf,
                            document.DocumentTypeId, versionId, originalData, originalFilesName, cancellationToken);
                        if (apiUpdated) updated++;
                    }

                    if (processingOptions.CreateBackup)
                    {
                        await _fileStorage.SaveFileAsync(processedPdf, $"{documentId}_processed", "processed", cancellationToken);
                    }

                    await LogAsync("SUCCESS", "Procesamiento",
                        anyPageRemoved ? "Documento procesado (páginas removidas)" : "Documento procesado (sin cambios)", documentId);

                    processed++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error procesando documento {DocumentId}", documentId);
                    await LogAsync("ERROR", "Procesamiento", $"Error: {ex.Message}", documentId);
                }

                // Actualizar progreso después de cada documento
                _progressStore.UpdateProgress(sessionId, new ProcessProgressDto
                {
                    Total = total,
                    Processed = processed,
                    Updated = updated,
                    PagesRemoved = pagesRemoved,
                    Errors = errors,
                    Skipped = skipped,
                    CurrentDocumentId = documentId,
                    CurrentDocumentName = documentName,
                    Status = $"Procesados {processed}/{total}"
                });
            }

            // Progreso final
            _progressStore.UpdateProgress(sessionId, new ProcessProgressDto
            {
                Total = total,
                Processed = processed,
                Updated = updated,
                PagesRemoved = pagesRemoved,
                Errors = errors,
                Skipped = skipped,
                Status = "Completado"
            });

            string summary = $"Procesamiento finalizado. Procesados: {processed}, Actualizados: {updated}, Páginas removidas: {pagesRemoved}, Errores: {errors}, Saltados: {skipped}";
            _logger.LogInformation(summary);
            await LogAsync("INFO", "Procesamiento", summary);
        }

        /// <summary>
        /// Determina qué páginas deben eliminarse según la configuración y las reglas de análisis.
        /// </summary>
        private async Task<List<int>> DeterminePagesToRemoveAsync(
            byte[] pdfBytes,
            ProcessingOptions options,
            AnalysisRuleSet rules,
            CancellationToken cancellationToken)
        {
            var pagesToRemove = new List<int>();
            int totalPages = await _pdfManipulator.GetPageCountAsync(pdfBytes, cancellationToken);
            bool hasRules = HasActiveRules(rules);

            // Modo automático: analizar todas las páginas y eliminar las que cumplan criterios
            if (options.AutoRemoveAllSeparators)
            {
                _logger.LogInformation("Modo automático: analizando todas las {TotalPages} páginas", totalPages);
                for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                {
                    var result = await _pdfAnalyzer.AnalyzePageAsync(pdfBytes, pageIdx, rules, cancellationToken);
                    if (result.ShouldRemove)
                    {
                        pagesToRemove.Add(pageIdx);
                        _logger.LogDebug("Página {Page} marcada para eliminar (contiene separador)", pageIdx + 1);
                    }
                }
                return pagesToRemove;
            }

            // Modo manual: obtener páginas candidatas según configuración (rangos o específicas)
            if (!options.RemovePagesEnabled)
                return pagesToRemove;

            var candidatePages = ParsePageRange(options, totalPages);
            if (!candidatePages.Any())
                return pagesToRemove;

            _logger.LogInformation("Modo manual: {Count} páginas candidatas: [{Pages}]",
                candidatePages.Count, string.Join(",", candidatePages.Select(p => p + 1)));

            // Si no hay reglas de análisis, eliminamos directamente las candidatas (comportamiento original)
            if (!hasRules)
            {
                _logger.LogWarning("No hay reglas de análisis definidas. Se eliminarán todas las páginas candidatas sin verificación.");
                pagesToRemove.AddRange(candidatePages);
                return pagesToRemove;
            }

            // Hay reglas: decidimos si analizar todas o solo las candidatas según AnalyzeAllPages
            if (options.AnalyzeAllPages)
            {
                // Analizar todas las páginas (no solo candidatas) y eliminar las que cumplan criterios
                _logger.LogInformation("AnalyzeAllPages=true: analizando todas las páginas con reglas");
                for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                {
                    var result = await _pdfAnalyzer.AnalyzePageAsync(pdfBytes, pageIdx, rules, cancellationToken);
                    if (result.ShouldRemove)
                    {
                        pagesToRemove.Add(pageIdx);
                        _logger.LogDebug("Página {Page} marcada para eliminar (contiene separador)", pageIdx + 1);
                    }
                }
            }
            else
            {
                // Analizar solo las páginas candidatas y eliminar aquellas que cumplan criterios
                _logger.LogInformation("AnalyzeAllPages=false: analizando solo páginas candidatas con reglas");
                foreach (int pageIdx in candidatePages)
                {
                    var result = await _pdfAnalyzer.AnalyzePageAsync(pdfBytes, pageIdx, rules, cancellationToken);
                    if (result.ShouldRemove)
                    {
                        pagesToRemove.Add(pageIdx);
                        _logger.LogDebug("Página candidata {Page} contiene separador -> eliminar", pageIdx + 1);
                    }
                    else
                    {
                        _logger.LogDebug("Página candidata {Page} NO contiene separador -> conservar", pageIdx + 1);
                    }
                }
            }

            return pagesToRemove;
        }

        private bool HasActiveRules(AnalysisRuleSet rules)
        {
            return rules != null &&
                   (rules.KeywordCodigo?.Any() == true ||
                    rules.KeywordSeparador?.Any() == true ||
                    rules.SearchCharacterLimit > 0);
        }

        private async Task<string> GetLatestVersionIdAsync(ProductivityDocumentDto document, string documentId, StoredConfiguration config, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(document.LastDocumentVersionId))
                return document.LastDocumentVersionId;

            var versions = await _apiClient.GetDocumentVersionsAsync(config.ApiBaseUrl, config.BearerToken, documentId, cancellationToken);
            var lastVersion = versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            if (lastVersion?.DocumentVersionId == null)
                throw new Exception("No se pudo obtener ninguna versión del documento");
            return lastVersion.DocumentVersionId;
        }

        private async Task LogAsync(string level, string category, string message, string documentId = null)
        {
            _logger.Log(GetLogLevel(level), message);
        }

        private static LogLevel GetLogLevel(string level) => level switch
        {
            "ERROR" => LogLevel.Error,
            "WARNING" => LogLevel.Warning,
            "SUCCESS" => LogLevel.Information,
            _ => LogLevel.Information
        };

        private byte[] DataUrlToBytes(string dataUrl)
        {
            var base64Data = dataUrl.Substring(dataUrl.IndexOf(",") + 1);
            return Convert.FromBase64String(base64Data);
        }

        private List<int> ParsePageRange(ProcessingOptions options, int totalPages)
        {
            var pages = new List<int>();
            if (options.RemoveMode == "specific" && !string.IsNullOrWhiteSpace(options.PagesToRemove))
            {
                var parts = options.PagesToRemove.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (trimmed.Contains('-'))
                    {
                        var range = trimmed.Split('-');
                        if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                        {
                            start = Math.Max(1, start);
                            end = Math.Min(totalPages, end);
                            for (int p = start; p <= end; p++)
                                pages.Add(p - 1);
                        }
                    }
                    else if (int.TryParse(trimmed, out int single))
                    {
                        if (single >= 1 && single <= totalPages)
                            pages.Add(single - 1);
                    }
                }
            }
            else if (options.RemoveMode == "range")
            {
                int start = Math.Max(1, options.PageRangeStart);
                int end = Math.Min(totalPages, options.PageRangeEnd);
                for (int p = start; p <= end; p++)
                    pages.Add(p - 1);
            }
            return pages.Distinct().OrderBy(x => x).ToList();
        }
    }
}