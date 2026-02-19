using AutoMapper;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
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
        private readonly IActivityLogRepository _logRepository;
        private readonly IProcessedDocumentRepository _processedDocumentRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly IPdfAnalyzer _pdfAnalyzer;
        private readonly IPdfManipulator _pdfManipulator;
        private readonly IFileStorageService _fileStorage;
        private readonly IProcessingProgressStore _progressStore;
        private readonly ILogger<ProcessingService> _logger;

        public ProcessingService(
            IStoredConfigurationRepository configurationRepository,
            IActivityLogRepository logRepository,
            IProcessedDocumentRepository processedDocumentRepository,
            IProductivityApiClient apiClient,
            IPdfAnalyzer pdfAnalyzer,
            IPdfManipulator pdfManipulator,
            IFileStorageService fileStorage,
            IProcessingProgressStore progressStore,
            ILogger<ProcessingService> logger)
        {
            _configurationRepository = configurationRepository;
            _logRepository = logRepository;
            _processedDocumentRepository = processedDocumentRepository;
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
            // 1. Obtener configuración activa (incluye credenciales API, opciones de procesamiento y reglas de análisis)
            var config = await _configurationRepository.GetActiveConfigurationAsync();
            var processingOptions = config.ProcessingOptions ?? new ProcessingOptions();
            var analysisRules = config.AnalysisRules ?? new AnalysisRuleSet();

            // Aplicar sobreescritura de opciones si vienen en la request
            bool updateApi = request.UpdateApi ?? processingOptions.UpdateApi;
            bool saveOriginals = request.SaveOriginals ?? processingOptions.SaveOriginalFiles;

            int total = request.DocumentIds.Count;
            int processed = 0, updated = 0, pagesRemoved = 0, errors = 0, skipped = 0;

            // Inicializar progreso en store
            var initialProgress = new ProcessProgressDto
            {
                Total = total,
                Processed = 0,
                Updated = 0,
                PagesRemoved = 0,
                Errors = 0,
                Skipped = 0,
                CurrentDocumentName = "Iniciando...",
                Status = "Iniciando"
            };
            _progressStore.UpdateProgress(sessionId, initialProgress);

            // Log inicial
            await _logRepository.SaveEntityAsync(new ActivityLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "INFO",
                Category = "Procesamiento",
                Message = $"Iniciando procesamiento de {total} documento(s) (sesión: {sessionId})"
            });

            // 2. Iterar sobre cada documento solicitado
            for (int i = 0; i < request.DocumentIds.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Procesamiento cancelado por usuario para sesión {SessionId}", sessionId);
                    await _logRepository.SaveEntityAsync(new ActivityLogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Level = "WARNING",
                        Category = "Procesamiento",
                        Message = $"Procesamiento cancelado por el usuario (sesión: {sessionId})"
                    });
                    break;
                }

                var documentId = request.DocumentIds[i];
                string documentName = string.Empty;

                try
                {
                    // 2.1 Obtener información del documento (para nombre, tipo y versión)
                    _logger.LogDebug("Obteniendo información del documento {DocumentId}", documentId);
                    var document = await _apiClient.GetDocumentAsync(
                        config.ApiBaseUrl,
                        config.BearerToken,
                        documentId,
                        cancellationToken);

                    documentName = document.Name;

                    // Actualizar progreso: documento actual
                    var currentProgress = new ProcessProgressDto
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
                    };
                    _progressStore.UpdateProgress(sessionId, currentProgress);

                    // 2.2 Obtener la última versión del documento
                    string versionId = document.LastDocumentVersionId;
                    if (string.IsNullOrEmpty(versionId))
                    {
                        _logger.LogInformation("Documento {DocumentId} sin versión directa, consultando versiones", documentId);
                        var versions = await _apiClient.GetDocumentVersionsAsync(
                            config.ApiBaseUrl,
                            config.BearerToken,
                            documentId,
                            cancellationToken);

                        var lastVersion = versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                        versionId = lastVersion?.DocumentVersionId;

                        if (string.IsNullOrEmpty(versionId))
                        {
                            throw new Exception("No se pudo obtener ninguna versión del documento");
                        }
                    }

                    // 2.3 Descargar el PDF de esa versión
                    _logger.LogDebug("Descargando PDF del documento {DocumentId}, versión {VersionId}", documentId, versionId);
                    byte[] pdfBytes = await _apiClient.DownloadPdfAsync(
                        config.ApiBaseUrl,
                        config.BearerToken,
                        versionId,
                        cancellationToken);

                    if (pdfBytes == null || pdfBytes.Length == 0)
                    {
                        throw new Exception("El PDF descargado está vacío");
                    }

                    // 2.4 Guardar original si está configurado
                    string originalPath = null;
                    if (saveOriginals)
                    {
                        originalPath = await _fileStorage.SaveFileAsync(
                            pdfBytes,
                            documentId,
                            documentName,
                            cancellationToken);
                        _logger.LogDebug("Original guardado en {Path}", originalPath);
                    }

                    // 2.5 Analizar la primera página con las reglas configuradas
                    var analysisResult = await _pdfAnalyzer.AnalyzePdfAsync(pdfBytes, analysisRules, cancellationToken);
                    bool shouldRemove = analysisResult.ShouldRemove;

                    _logger.LogDebug("Análisis del documento {DocumentId}: {Diagnosis}", documentId, analysisResult.Diagnosis);

                    // 2.6 Decidir si se procesa o se salta
                    bool pageRemoved = false;
                    byte[] processedPdf = pdfBytes;

                    if (processingOptions.RemoveFirstPage)
                    {
                        if (processingOptions.OnlyIfCriteriaMet && !shouldRemove)
                        {
                            // No cumple criterios → se salta
                            skipped++;
                            await _logRepository.SaveEntityAsync(new ActivityLogEntry
                            {
                                Timestamp = DateTime.UtcNow,
                                Level = "INFO",
                                Category = "Procesamiento",
                                DocumentId = documentId,
                                Message = $"Documento saltado: no cumple criterios de análisis"
                            });
                            processed++; // Contamos como procesado para el progreso (aunque no se modificó)
                            continue;
                        }

                        // Eliminar primera página
                        processedPdf = await _pdfManipulator.RemoveFirstPageAsync(pdfBytes, cancellationToken);

                        // Verificar si realmente se eliminó (el manipulador devuelve el original si no pudo o si tenía 1 página)
                        if (processedPdf.Length != pdfBytes.Length)
                        {
                            pageRemoved = true;
                            pagesRemoved++;
                            _logger.LogDebug("Primera página removida del documento {DocumentId}", documentId);
                        }
                    }

                    // 2.7 Guardar PDF procesado (si se modificó o no, siempre lo guardamos para tener registro)
                    string processedPath = await _fileStorage.SaveFileAsync(
                        processedPdf,
                        documentId,
                        documentName,
                        cancellationToken);

                    // 2.8 Actualizar en API si corresponde (solo si se removió página y updateApi=true)
                    bool apiUpdated = false;
                    if (updateApi && pageRemoved)
                    {
                        _logger.LogDebug("Subiendo PDF procesado a la API para documento {DocumentId}", documentId);
                        apiUpdated = await _apiClient.UploadPdfAsync(
                            config.ApiBaseUrl,
                            config.BearerToken,
                            documentName,
                            processedPdf,
                            document.DocumentTypeId,
                            versionId, // versión padre para crear nueva versión
                            cancellationToken);

                        if (apiUpdated)
                        {
                            updated++;
                            _logger.LogDebug("Documento {DocumentId} actualizado en API", documentId);
                        }
                        else
                        {
                            _logger.LogWarning("Fallo al actualizar documento {DocumentId} en API", documentId);
                            // No incrementamos errors aquí porque el procesamiento local fue exitoso,
                            // pero podríamos contar como error parcial si se desea.
                        }
                    }

                    // 2.9 Registrar resultado en base de datos local
                    var processedDoc = new ProcessedDocument
                    {
                        DocumentId = documentId,
                        OriginalFileName = documentName,
                        ProcessedFilePath = processedPath,
                        OriginalFilePath = originalPath,
                        PagesRemoved = pageRemoved ? 1 : 0,
                        ApiUpdated = apiUpdated,
                        ProcessingDate = DateTime.UtcNow,
                        ErrorMessage = null
                    };
                    await _processedDocumentRepository.SaveEntityAsync(processedDoc);

                    await _logRepository.SaveEntityAsync(new ActivityLogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Level = "SUCCESS",
                        Category = "Procesamiento",
                        DocumentId = documentId,
                        Message = pageRemoved ? "Documento procesado (página removida)" : "Documento procesado (sin cambios)"
                    });

                    processed++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error procesando documento {DocumentId} en sesión {SessionId}", documentId, sessionId);

                    await _logRepository.SaveEntityAsync(new ActivityLogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Level = "ERROR",
                        Category = "Procesamiento",
                        DocumentId = documentId,
                        Message = $"Error: {ex.Message}"
                    });

                    // Registrar el fallo en ProcessedDocument (opcional)
                    var failedDoc = new ProcessedDocument
                    {
                        DocumentId = documentId,
                        OriginalFileName = documentName,
                        ProcessingDate = DateTime.UtcNow,
                        ErrorMessage = ex.Message
                    };
                    await _processedDocumentRepository.SaveEntityAsync(failedDoc);
                }

                // Actualizar progreso después de cada documento
                var afterProgress = new ProcessProgressDto
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
                };
                _progressStore.UpdateProgress(sessionId, afterProgress);
            }

            // 3. Progreso final
            var finalProgress = new ProcessProgressDto
            {
                Total = total,
                Processed = processed,
                Updated = updated,
                PagesRemoved = pagesRemoved,
                Errors = errors,
                Skipped = skipped,
                Status = "Completado"
            };
            _progressStore.UpdateProgress(sessionId, finalProgress);

            string summary = $"Procesamiento finalizado. Procesados: {processed}, Actualizados: {updated}, Páginas removidas: {pagesRemoved}, Errores: {errors}, Saltados: {skipped}";
            _logger.LogInformation(summary);
            await _logRepository.SaveEntityAsync(new ActivityLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "INFO",
                Category = "Procesamiento",
                Message = summary
            });
        }
    }
}