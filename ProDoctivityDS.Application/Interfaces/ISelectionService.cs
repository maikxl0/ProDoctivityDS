namespace ProDoctivityDS.Application.Interfaces
{
    public interface ISelectionService
    {
        /// <summary>
        /// Selecciona una lista de documentos para una sesión.
        /// </summary>
        Task SelectDocumentsAsync(string sessionId, IEnumerable<string> documentIds);

        /// <summary>
        /// Deselecciona una lista de documentos para una sesión.
        /// </summary>
        Task DeselectDocumentsAsync(string sessionId, IEnumerable<string> documentIds);

        /// <summary>
        /// Obtiene todos los documentos seleccionados en la sesión.
        /// </summary>
        Task<IEnumerable<string>> GetSelectedDocumentsAsync(string sessionId);

        /// <summary>
        /// Obtiene la cantidad de documentos seleccionados.
        /// </summary>
        Task<int> GetSelectedCountAsync(string sessionId);

        /// <summary>
        /// Selecciona todos los documentos de la página actual (agrega a los ya seleccionados).
        /// </summary>
        /// <param name="sessionId">Identificador de sesión.</param>
        /// <param name="pageDocumentIds">Lista de IDs de documentos visibles en la página actual.</param>
        Task SelectAllCurrentPageAsync(string sessionId, IEnumerable<string> pageDocumentIds);

        /// <summary>
        /// Deselecciona todos los documentos de la página actual (los quita de la selección, dejando otros).
        /// </summary>
        Task DeselectAllCurrentPageAsync(string sessionId, IEnumerable<string> pageDocumentIds);

        /// <summary>
        /// Invierte la selección en la página actual (los seleccionados se deseleccionan y viceversa).
        /// </summary>
        Task InvertSelectionCurrentPageAsync(string sessionId, IEnumerable<string> pageDocumentIds);

        /// <summary>
        /// Obtiene los Type IDs únicos de los documentos seleccionados, dado un mapa de documentId -> typeId.
        /// </summary>
        /// <param name="sessionId">Identificador de sesión.</param>
        /// <param name="documentTypeMap">Diccionario que mapea documentId a su typeId.</param>
        Task<IEnumerable<string>> GetSelectedTypeIdsAsync(string sessionId, IDictionary<string, string> documentTypeMap);

        /// <summary>
        /// Obtiene los Type IDs como texto (uno por línea) para copiar al portapapeles.
        /// </summary>
        Task<string> GetSelectedTypeIdsTextAsync(string sessionId, IDictionary<string, string> documentTypeMap);

        /// <summary>
        /// Elimina toda la selección de la sesión.
        /// </summary>
        Task ClearSelectionAsync(string sessionId);
    }
}