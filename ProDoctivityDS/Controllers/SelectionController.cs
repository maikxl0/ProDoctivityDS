using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Interfaces;


namespace ProDoctivityDS.Controllers
{
    /// <summary>
    /// Controlador para gestionar la selección de documentos por sesión de usuario.
    /// Requiere header X-Session-Id para identificar la sesión.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SelectionController : ControllerBase
    {
        private readonly ISelectionService _selectionService;
        private readonly ILogger<SelectionController> _logger;

        public SelectionController(ISelectionService selectionService, ILogger<SelectionController> logger)
        {
            _selectionService = selectionService;
            _logger = logger;
        }
        private string GetOrCreateSessionId()
        {
            if (Request.Headers.TryGetValue("X-Session-Id", out var sessionId))
                return sessionId.ToString();

            var newSessionId = Guid.NewGuid().ToString();
            Response.Headers.Append("X-Session-Id", newSessionId);
            return newSessionId;
        }

        /// <summary>
        /// Agrega documentos a la selección actual de la sesión.
        /// </summary>
        /// <param name="documentIds">Lista de IDs de documentos a seleccionar</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Documentos seleccionados correctamente</response>
        /// <response code="400">Lista de IDs inválida</response>
        [HttpPost("select")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SelectDocuments([FromBody] List<string> documentIds)
        {
            var SessionId = GetOrCreateSessionId();
            _logger.LogInformation("SelectDocuments - SessionId recibido: {SessionId}", SessionId);
            _logger.LogInformation("SelectDocuments - IDs a seleccionar: {Ids}", string.Join(",", documentIds));

            if (documentIds == null || !documentIds.Any())
                return BadRequest(new { message = "La lista de IDs no puede estar vacía" });

            await _selectionService.SelectDocumentsAsync(SessionId, documentIds);
            _logger.LogDebug("Documentos seleccionados para sesión {SessionId}: {Count}", SessionId, documentIds.Count);
            return Ok(new { message = "Documentos seleccionados correctamente" });
        }

        /// <summary>
        /// Remueve documentos de la selección actual de la sesión.
        /// </summary>
        /// <param name="documentIds">Lista de IDs de documentos a deseleccionar</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Documentos deseleccionados correctamente</response>
        /// <response code="400">Lista de IDs inválida</response>
        [HttpPost("deselect")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeselectDocuments([FromBody] List<string> documentIds)
        {
            var SessionId = GetOrCreateSessionId();
            _logger.LogInformation("SelectDocuments - SessionId recibido: {SessionId}", SessionId);
            _logger.LogInformation("SelectDocuments - IDs a seleccionar: {Ids}", string.Join(",", documentIds));

            if (documentIds == null || !documentIds.Any())
                return BadRequest(new { message = "La lista de IDs no puede estar vacía" });

            await _selectionService.DeselectDocumentsAsync(SessionId, documentIds);
            _logger.LogDebug("Documentos deseleccionados para sesión {SessionId}: {Count}", SessionId, documentIds.Count);
            return Ok(new { message = "Documentos deseleccionados correctamente" });
        }

        /// <summary>
        /// Obtiene la lista de IDs de documentos seleccionados en la sesión actual.
        /// </summary>
        /// <returns>Lista de IDs seleccionados</returns>
        /// <response code="200">Lista de IDs (puede estar vacía)</response>
        [HttpGet("selected")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        public async Task<ActionResult<IEnumerable<string>>> GetSelectedDocuments()
        {
            var SessionId = GetOrCreateSessionId();
            _logger.LogInformation("SelectDocuments - SessionId recibido: {SessionId}", SessionId);

            var selected = await _selectionService.GetSelectedDocumentsAsync(SessionId);
            return Ok(selected);
        }

        /// <summary>
        /// Obtiene el número de documentos seleccionados en la sesión actual.
        /// </summary>
        /// <returns>Contador de seleccionados</returns>
        /// <response code="200">Contador de seleccionados</response>
        [HttpGet("selected-count")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<ActionResult<int>> GetSelectedCount()
        {
            var SessionId = GetOrCreateSessionId();
            _logger.LogInformation("SelectDocuments - SessionId recibido: {SessionId}", SessionId);

            var count = await _selectionService.GetSelectedCountAsync(SessionId);
            return Ok(count);
        }


        /// <summary>
        /// Selecciona todos los documentos de la página actual (agrega a los ya seleccionados).
        /// </summary>
        /// <param name="pageDocumentIds">Lista de IDs de documentos visibles en la página actual</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Selección de página completada</response>
        /// <response code="400">Lista de IDs inválida</response>
        [HttpPost("select-all-page")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SelectAllPage([FromBody] List<string> pageDocumentIds)
        {
            var SessionId = GetOrCreateSessionId();
            _logger.LogInformation("SelectDocuments - SessionId recibido: {SessionId}", SessionId);
            _logger.LogInformation("SelectDocuments - IDs a seleccionar: {Ids}", string.Join(",", pageDocumentIds));

            if (pageDocumentIds == null || !pageDocumentIds.Any())
                return BadRequest(new { message = "La lista de IDs de página no puede estar vacía" });

            await _selectionService.SelectAllCurrentPageAsync(SessionId, pageDocumentIds);
            _logger.LogDebug("Seleccionados todos los documentos de página para sesión {SessionId}, {Count} documentos", SessionId, pageDocumentIds.Count);
            return Ok(new { message = "Selección de página completada" });
        }

        /// <summary>
        /// Deselecciona todos los documentos de la página actual (los quita de la selección).
        /// </summary>
        /// <param name="pageDocumentIds">Lista de IDs de documentos visibles en la página actual</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Deselección de página completada</response>
        /// <response code="400">Lista de IDs inválida</response>
        [HttpPost("deselect-all-page")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeselectAllPage([FromBody] List<string> pageDocumentIds)
        {
            var SessionId = GetOrCreateSessionId();
            _logger.LogInformation("SelectDocuments - SessionId recibido: {SessionId}", SessionId);
            _logger.LogInformation("SelectDocuments - IDs a seleccionar: {Ids}", string.Join(",", pageDocumentIds));


            if (pageDocumentIds == null || !pageDocumentIds.Any())
                return BadRequest(new { message = "La lista de IDs de página no puede estar vacía" });

            await _selectionService.DeselectAllCurrentPageAsync(SessionId, pageDocumentIds);
            _logger.LogDebug("Deseleccionados todos los documentos de página para sesión {SessionId}", SessionId);
            return Ok(new { message = "Deselección de página completada" });
        }

        /// <summary>
        /// Invierte la selección en la página actual.
        /// </summary>
        /// <param name="pageDocumentIds">Lista de IDs de documentos visibles en la página actual</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Inversión completada</response>
        /// <response code="400">Lista de IDs inválida</response>
        [HttpPost("invert-page")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> InvertPageSelection([FromBody] List<string> pageDocumentIds)
        {
            var SessionId = GetOrCreateSessionId();
            if (pageDocumentIds == null || !pageDocumentIds.Any())
                return BadRequest(new { message = "La lista de IDs de página no puede estar vacía" });

            await _selectionService.InvertSelectionCurrentPageAsync(SessionId, pageDocumentIds);
            _logger.LogDebug("Invertida selección de página para sesión {SessionId}", SessionId);
            return Ok(new { message = "Inversión completada" });
        }

        /// <summary>
        /// Obtiene los Type IDs únicos de los documentos seleccionados.
        /// Requiere un mapa de documentId → typeId proporcionado por el frontend.
        /// </summary>
        /// <param name="documentTypeMap">Diccionario con pares documentId:typeId (en query string, ej. ?doc1=type1&amp;doc2=type2)</param>
        /// <returns>Lista de Type IDs únicos</returns>
        /// <response code="200">Lista de Type IDs</response>
        /// <response code="400">Mapa inválido o vacío</response>
        [HttpGet("type-ids")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<string>>> GetSelectedTypeIds([FromQuery] Dictionary<string, string> documentTypeMap)
        {
            var SessionId = GetOrCreateSessionId();

            _logger.LogInformation("SelectDocuments - SessionId recibido: {SessionId}", SessionId);

            if (documentTypeMap == null || !documentTypeMap.Any())
                return BadRequest(new { message = "El mapa documentId->typeId no puede estar vacío" });

            var typeIds = await _selectionService.GetSelectedTypeIdsAsync(SessionId, documentTypeMap);
            return Ok(typeIds);
        }

        /// <summary>
        /// Elimina toda la selección de documentos de la sesión actual.
        /// </summary>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Selección eliminada correctamente</response>
        [HttpDelete("clear")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ClearAllSelection()
        {
            var sessionId = GetOrCreateSessionId();
            await _selectionService.ClearSelectionAsync(sessionId);
            _logger.LogDebug("Selección limpiada para sesión {SessionId}", sessionId);
            return Ok(new { message = "Selección eliminada correctamente" });
        }


    }
}