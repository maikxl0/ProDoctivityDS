using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    /// <summary>
    /// Controlador para gestionar la configuración del sistema (API, procesamiento, reglas de análisis)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(IConfigurationService configurationService, ILogger<ConfigurationController> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la configuración activa (los campos sensibles se muestran ocultos)
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Configuración actual del sistema</returns>
        /// <response code="200">Configuración obtenida correctamente</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet]
        [ProducesResponseType(typeof(ConfigurationDto), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ConfigurationDto>> GetConfiguration(CancellationToken cancellationToken)
        {
            try
            {
                var config = await _configurationService.GetConfigurationDtoAsync(cancellationToken);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener configuración");
                return StatusCode(500, new { message = "Error interno al obtener configuración" });
            }
        }

        /// <summary>
        /// Guarda la configuración completa (credenciales, opciones de procesamiento y reglas de análisis)
        /// </summary>
        /// <param name="request">Datos de configuración a guardar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Configuración guardada correctamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPut]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SaveConfiguration([FromBody] SaveConfigurationRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _configurationService.SaveConfigurationAsync(request, cancellationToken);
                return Ok(new { message = "Configuración guardada exitosamente" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Datos de configuración inválidos");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración");
                return StatusCode(500, new { message = "Error interno al guardar configuración" });
            }
        }

        /// <summary>
        /// Prueba la conexión a la API de Productivity con las credenciales proporcionadas
        /// </summary>
        /// <param name="credentials">Credenciales a probar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la prueba (true/false)</returns>
        /// <response code="200">Prueba ejecutada, devuelve éxito o fallo</response>
        /// <response code="400">Credenciales inválidas</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPost("test-connection")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<bool>> TestConnection([FromBody] ApiCredentialsDto credentials, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _configurationService.TestConnectionAsync(credentials, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al probar conexión");
                return StatusCode(500, new { message = "Error interno al probar conexión" });
            }
        }

        /// <summary>
        /// Exporta la configuración actual incluyendo credenciales en texto plano (para descargar como JSON)
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Configuración completa con credenciales visibles</returns>
        /// <response code="200">Configuración exportada correctamente</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("export")]
        [ProducesResponseType(typeof(ConfigurationDto), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ConfigurationDto>> ExportConfiguration(CancellationToken cancellationToken)
        {
            try
            {
                var config = await _configurationService.ExportConfigurationAsync(cancellationToken);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar configuración");
                return StatusCode(500, new { message = "Error interno al exportar configuración" });
            }
        }

        /// <summary>
        /// Importa una configuración desde un JSON (reemplaza la configuración actual)
        /// </summary>
        /// <param name="importDto">Configuración a importar (con credenciales en texto plano)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Configuración importada correctamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPost("import")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ImportConfiguration([FromBody] ConfigurationDto importDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _configurationService.ImportConfigurationAsync(importDto, cancellationToken);
                return Ok(new { message = "Configuración importada exitosamente" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Datos de importación inválidos");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar configuración");
                return StatusCode(500, new { message = "Error interno al importar configuración" });
            }
        }
    }
}