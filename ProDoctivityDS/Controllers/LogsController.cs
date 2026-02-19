using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    /// <summary>
    /// Controlador para consultar y gestionar los logs de actividad del sistema
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ILogService logService, ILogger<LogsController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene los logs más recientes, opcionalmente filtrados por nivel y con límite.
        /// </summary>
        /// <param name="level">Filtrar por nivel (INFO, SUCCESS, WARNING, ERROR, DEBUG)</param>
        /// <param name="limit">Número máximo de registros a retornar (por defecto 100, máximo 500)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de logs</returns>
        /// <response code="200">Logs obtenidos correctamente</response>
        /// <response code="400">Parámetros inválidos</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogEntryDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<ActivityLogEntryDto>>> GetLogs(
            [FromQuery] string? level,
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default)
        {
            if (limit < 1 || limit > 500)
                return BadRequest(new { message = "El límite debe estar entre 1 y 500" });

            try
            {
                IEnumerable<ActivityLogEntryDto> logs;

                if (!string.IsNullOrWhiteSpace(level))
                {
                    // Validar nivel
                    var validLevels = new[] { "INFO", "SUCCESS", "WARNING", "ERROR", "DEBUG" };
                    if (!Array.Exists(validLevels, l => l.Equals(level, StringComparison.OrdinalIgnoreCase)))
                        return BadRequest(new { message = $"Nivel inválido. Valores permitidos: {string.Join(", ", validLevels)}" });

                    logs = await _logService.GetLogsByLevelAsync(level, limit, cancellationToken);
                }
                else
                {
                    logs = await _logService.GetRecentLogsAsync(limit, cancellationToken);
                }

                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener logs");
                return StatusCode(500, new { message = "Error interno al obtener logs" });
            }
        }

        /// <summary>
        /// Obtiene los logs asociados a un documento específico.
        /// </summary>
        /// <param name="documentId">ID del documento</param>
        /// <param name="limit">Número máximo de registros (por defecto 100)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de logs del documento</returns>
        /// <response code="200">Logs obtenidos correctamente</response>
        /// <response code="400">ID de documento inválido</response>
        /// <response code="404">Documento no encontrado (opcional, pero se retorna lista vacía si no hay logs)</response>
        [HttpGet("document/{documentId}")]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogEntryDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<ActivityLogEntryDto>>> GetLogsByDocument(
            string documentId,
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest(new { message = "El ID del documento es requerido" });

            if (limit < 1 || limit > 500)
                return BadRequest(new { message = "El límite debe estar entre 1 y 500" });

            try
            {
                var logs = await _logService.GetLogsByDocumentIdAsync(documentId, limit, cancellationToken);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener logs del documento {DocumentId}", documentId);
                return StatusCode(500, new { message = "Error interno al obtener logs" });
            }
        }

        /// <summary>
        /// Elimina todos los logs del sistema (solo para administración).
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="204">Logs eliminados correctamente</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpDelete]
        [ProducesResponseType(204)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ClearLogs(CancellationToken cancellationToken)
        {
            try
            {
                await _logService.ClearLogsAsync(cancellationToken);
                _logger.LogInformation("Todos los logs han sido eliminados");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar logs");
                return StatusCode(500, new { message = "Error interno al eliminar logs" });
            }
        }
    }
}