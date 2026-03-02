using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentTypesController : ControllerBase
    {
        private readonly IDocumentTypeService _documentTypeService;
        private readonly ILogger<DocumentTypesController> _logger;

        public DocumentTypesController(
            IDocumentTypeService documentTypeService,
            ILogger<DocumentTypesController> logger)
        {
            _documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<DocumentTypeDto>>> GetDocumentTypes(CancellationToken cancellationToken)
        {
            try
            {
                var types = await _documentTypeService.GetAllDocumentTypes(cancellationToken);
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de documento");
                return StatusCode(500, new { message = "Error interno al obtener tipos de documento" });
            }
        }
    }
}