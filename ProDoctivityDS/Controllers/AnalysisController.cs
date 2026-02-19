using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Controllers
{
    /// <summary>
    /// Controlador para la configuración y prueba de reglas de análisis de PDF
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly IAnalysisService _analysisService;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(IAnalysisService analysisService, ILogger<AnalysisController> logger)
        {
            _analysisService = analysisService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las reglas de análisis actualmente configuradas
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Reglas de análisis (criterios, normalización, etc.)</returns>
        /// <response code="200">Reglas obtenidas correctamente</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("rules")]
        [ProducesResponseType(typeof(AnalysisRuleSetDto), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<AnalysisRuleSetDto>> GetRules(CancellationToken cancellationToken)
        {
            try
            {
                var rules = await _analysisService.GetCurrentRulesAsync(cancellationToken);
                return Ok(rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener reglas de análisis");
                return StatusCode(500, new { message = "Error interno al obtener reglas" });
            }
        }

        /// <summary>
        /// Actualiza las reglas de análisis
        /// </summary>
        /// <param name="rulesDto">Nuevas reglas de análisis</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Reglas actualizadas correctamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPut("rules")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRules([FromBody] AnalysisRuleSetDto rulesDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _analysisService.SaveRulesAsync(rulesDto, cancellationToken);
                _logger.LogInformation("Reglas de análisis actualizadas");
                return Ok(new { message = "Reglas de análisis actualizadas correctamente" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Datos de reglas inválidos");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar reglas de análisis");
                return StatusCode(500, new { message = "Error interno al actualizar reglas" });
            }
        }

        /// <summary>
        /// Prueba las reglas de análisis actuales en un archivo PDF subido
        /// </summary>
        /// <param name="file">Archivo PDF a analizar (multipart/form-data)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado del análisis (si debe removerse y diagnóstico)</returns>
        /// <response code="200">Análisis completado</response>
        /// <response code="400">Archivo no válido o no es PDF</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPost("test")]
        [ProducesResponseType(typeof(AnalysisTestResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<AnalysisTestResponseDto>> TestAnalysis(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Debe proporcionar un archivo PDF" });

            // Validar extensión
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
                return BadRequest(new { message = "El archivo debe ser un PDF" });

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken);
                var fileBytes = memoryStream.ToArray();

                var request = new TestAnalysisRequestDto
                {
                    FileContent = fileBytes,
                    FileName = file.FileName
                };

                var result = await _analysisService.TestPdfAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al probar análisis en archivo {FileName}", file.FileName);
                return StatusCode(500, new { message = "Error interno al analizar el PDF" });
            }
        }
    }
}