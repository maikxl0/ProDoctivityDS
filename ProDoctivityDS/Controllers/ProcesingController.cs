using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    /// <summary>
    /// Controlador para el procesamiento de documentos (inicio, progreso, cancelación)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessingController : ControllerBase
    {
        private readonly IProcessingProgressStore _progressStore;
        private readonly ILogger<ProcessingController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISelectionService _selectionService;

        // Almacenamiento en memoria de los cancellation tokens por sesión
        private static readonly Dictionary<string, CancellationTokenSource> _activeProcesses = new();

        public ProcessingController(
            IProcessingProgressStore progressStore,
            ILogger<ProcessingController> logger,
            IServiceScopeFactory scopeFactory,
            ISelectionService selectionService)
        {
            _progressStore = progressStore;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _selectionService = selectionService;
        }

        private string GetOrCreateSessionId()
        {
            // Forzar la creación de la sesión si no existe
            if (Request.Headers.TryGetValue("X-Session-Id", out var sessionId))
            {
                _logger.LogInformation("Header X-Session-Id recibido: {SessionId}", sessionId);
                return sessionId.ToString();
            }

            var newSessionId = Guid.NewGuid().ToString();
            Response.Headers.Add("X-Session-Id", newSessionId);
            _logger.LogInformation("Nuevo X-Session-Id generado: {SessionId}", newSessionId);
            return newSessionId;
        }

        /// <summary>
        /// Inicia el procesamiento de los documentos seleccionados.
        /// El progreso puede consultarse mediante GET /progress.
        /// </summary>
        /// <param name="request">Lista de IDs de documentos a procesar y opciones específicas</param>
        /// <returns>Información de la sesión iniciada</returns>
        /// <response code="202">Procesamiento aceptado e iniciado</response>
        /// <response code="400">Solicitud inválida (lista vacía, etc.)</response>
        /// <response code="500">Error interno al iniciar</response>
        [HttpPost("start")]
        [ProducesResponseType(typeof(object), 202)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> StartProcessing([FromBody] ProcessRequestDto request)
        {
            var SessionId = GetOrCreateSessionId();
            // Validar entrada
            var selectedIds = (await _selectionService.GetSelectedDocumentsAsync(SessionId)).ToList();
            if (selectedIds.Count == 0)
                return BadRequest(new { message = "No hay documentos seleccionados para procesar" });

            if (request.DocumentIds.FirstOrDefault() == "string" || request.DocumentIds == null)
                request.DocumentIds = selectedIds; // Si viene vacio, tomar los seleccionados

            // Crear cancellation token
            var cts = new CancellationTokenSource();
            _activeProcesses[SessionId] = cts;

            // Ejecutar en segundo plano (fire-and-forget)
            _ = Task.Run(async () =>
            {
                // Crear un ámbito para esta tarea
                using (var scope = _scopeFactory.CreateScope())
                {
                    // Resolver IProcessingService dentro del ámbito
                    var processingService = scope.ServiceProvider.GetRequiredService<IProcessingService>();

                    try
                    {
                        _logger.LogInformation("Iniciando procesamiento para sesión {SessionId} con {Count} documentos",
                            SessionId, request.DocumentIds.Count);

                        
                        await processingService.ProcessDocumentsAsync(request, SessionId, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Procesamiento cancelado para sesión {SessionId}", SessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error en procesamiento para sesión {SessionId}", SessionId);

                        var errorProgress = new ProcessProgressDto
                        {
                            Total = request.DocumentIds.Count,
                            Processed = 0,
                            Updated = 0,
                            PagesRemoved = 0,
                            Errors = request.DocumentIds.Count,
                            Skipped = 0,
                            Status = $"Error crítico: {ex.Message}"
                        };
                        _progressStore.UpdateProgress(SessionId, errorProgress);
                    }
                    finally
                    {
                        _activeProcesses.Remove(SessionId);
                    }
                } // Al salir del using, el ámbito se destruye y el DbContext se libera
            }, CancellationToken.None);

            return Accepted(new { SessionId, message = "Procesamiento iniciado" });
        }


        /// <summary>
        /// Obtiene el progreso actual del procesamiento para la sesión actual.
        /// </summary>
        /// <returns>Estado del progreso</returns>
        /// <response code="200">Progreso obtenido (puede estar completo o en curso)</response>
        /// <response code="404">No hay proceso activo para esta sesión</response>
        [HttpGet("progress")]
        [ProducesResponseType(typeof(ProcessProgressDto), 200)]
        [ProducesResponseType(404)]
        public ActionResult<ProcessProgressDto> GetProgress()
        {
            var SessionId = GetOrCreateSessionId();
            var progress = _progressStore.GetProgress(SessionId);

            if (progress == null)
                return NotFound(new { message = "No hay proceso activo para esta sesión" });

            return Ok(progress);
        }

        /// <summary>
        /// Cancela el procesamiento en curso para la sesión actual.
        /// </summary>
        /// <returns>Resultado de la cancelación</returns>
        /// <response code="200">Cancelación solicitada correctamente</response>
        /// <response code="404">No hay proceso activo para cancelar</response>
        [HttpPost("cancel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult CancelProcessing()
        {
            var SessionId = GetOrCreateSessionId();
            if (_activeProcesses.TryGetValue(SessionId, out var cts))
            {
                cts.Cancel();
                _logger.LogInformation("Cancelación solicitada para sesión {SessionId}", SessionId);

                // Actualizar progreso para reflejar cancelación
                var progress = _progressStore.GetProgress(SessionId);
                if (progress != null)
                {
                    progress.Status = "Cancelado por usuario";
                    _progressStore.UpdateProgress(SessionId, progress);
                }

                return Ok(new { message = "Cancelación solicitada" });
            }

            return NotFound(new { message = "No hay proceso activo para esta sesión" });
        }

        /// <summary>
        /// Elimina el progreso almacenado para la sesión actual (útil cuando el frontend cierra la vista).
        /// </summary>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Progreso eliminado</response>
        [HttpDelete("progress")]
        [ProducesResponseType(200)]
        public IActionResult ClearProgress()
        {
            var SessionId = GetOrCreateSessionId();
            _progressStore.RemoveProgress(SessionId);
            _logger.LogDebug("Progreso eliminado para sesión {SessionId}", SessionId);
            return Ok(new { message = "Progreso eliminado" });
        }
    }
}