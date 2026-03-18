using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DuplicateController : ControllerBase
    {
        private readonly IDuplicateDetectionService _duplicateService;
        private readonly ILogger<DuplicateController> _logger;

        public DuplicateController(IDuplicateDetectionService duplicateService, ILogger<DuplicateController> logger)
        {
            _duplicateService = duplicateService;
            _logger = logger;
        }

        [HttpPost("check-by-cedula")]
        public async Task<ActionResult<DuplicateCheckResponse>> CheckByCedula([FromBody] DuplicateCheckRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Cedula))
                return BadRequest(new { message = "La cédula es requerida" });

            try
            {
                var result = await _duplicateService.CheckDuplicatesByCedulaAsync(request.Cedula, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar duplicados para cédula {Cedula}", request.Cedula);
                return StatusCode(500, new { message = "Error interno al procesar la solicitud" });
            }
        }
    }
}