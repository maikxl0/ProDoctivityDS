
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class SelectionService : ISelectionService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SelectionService> _logger;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;

        // Tiempo de expiración de la sesión (20 minutos de inactividad)
        private static readonly TimeSpan SessionSlidingExpiration = TimeSpan.FromMinutes(20);

        public SelectionService(IMemoryCache cache, ILogger<SelectionService> logger)
        {
            _cache = cache;
            _logger = logger;
            _cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(SessionSlidingExpiration);
        }

        private string GetCacheKey(string sessionId) => $"Selection_{sessionId}";

        private HashSet<string> GetOrCreateSelectionSet(string sessionId)
        {
            var key = GetCacheKey(sessionId);
            if (!_cache.TryGetValue(key, out HashSet<string> selection))
            {
                selection = new HashSet<string>();
                _cache.Set(key, selection, _cacheEntryOptions);
            }
            return selection;
        }

        private void UpdateCache(string sessionId, HashSet<string> selection)
        {
            var key = GetCacheKey(sessionId);
            _cache.Set(key, selection, _cacheEntryOptions);
        }

        /// <inheritdoc />
        public Task SelectDocumentsAsync(string sessionId, IEnumerable<string> documentIds)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var selection = GetOrCreateSelectionSet(sessionId);
            foreach (var docId in documentIds.Where(id => !string.IsNullOrEmpty(id)))
            {
                selection.Add(docId);
            }
            UpdateCache(sessionId, selection);
            _logger.LogDebug("Seleccionados {Count} documentos para sesión {SessionId}", documentIds.Count(), sessionId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeselectDocumentsAsync(string sessionId, IEnumerable<string> documentIds)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var selection = GetOrCreateSelectionSet(sessionId);
            foreach (var docId in documentIds.Where(id => !string.IsNullOrEmpty(id)))
            {
                selection.Remove(docId);
            }
            UpdateCache(sessionId, selection);
            _logger.LogDebug("Deseleccionados {Count} documentos para sesión {SessionId}", documentIds.Count(), sessionId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetSelectedDocumentsAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var selection = GetOrCreateSelectionSet(sessionId);
            return Task.FromResult(selection.AsEnumerable());
        }

        /// <inheritdoc />
        public Task<int> GetSelectedCountAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var selection = GetOrCreateSelectionSet(sessionId);
            return Task.FromResult(selection.Count);
        }

        /// <inheritdoc />
        public Task SelectAllCurrentPageAsync(string sessionId, IEnumerable<string> pageDocumentIds)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var selection = GetOrCreateSelectionSet(sessionId);
            foreach (var docId in pageDocumentIds.Where(id => !string.IsNullOrEmpty(id)))
            {
                selection.Add(docId);
            }
            UpdateCache(sessionId, selection);
            _logger.LogDebug("Seleccionados todos los documentos de la página actual para sesión {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeselectAllCurrentPageAsync(string sessionId, IEnumerable<string> pageDocumentIds)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var selection = GetOrCreateSelectionSet(sessionId);
            foreach (var docId in pageDocumentIds.Where(id => !string.IsNullOrEmpty(id)))
            {
                selection.Remove(docId);
            }
            UpdateCache(sessionId, selection);
            _logger.LogDebug("Deseleccionados todos los documentos de la página actual para sesión {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task InvertSelectionCurrentPageAsync(string sessionId, IEnumerable<string> pageDocumentIds)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var selection = GetOrCreateSelectionSet(sessionId);
            var pageIds = pageDocumentIds.Where(id => !string.IsNullOrEmpty(id)).ToHashSet();

            // Invertir: los que están en página y en selección se quitan; los que están en página pero no en selección se agregan
            foreach (var docId in pageIds)
            {
                if (selection.Contains(docId))
                    selection.Remove(docId);
                else
                    selection.Add(docId);
            }
            UpdateCache(sessionId, selection);
            _logger.LogDebug("Invertida selección en página actual para sesión {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetSelectedTypeIdsAsync(string sessionId, IDictionary<string, string> documentTypeMap)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");
            if (documentTypeMap == null)
                throw new ArgumentNullException(nameof(documentTypeMap));

            var selection = GetOrCreateSelectionSet(sessionId);
            var typeIds = selection
                .Where(docId => documentTypeMap.ContainsKey(docId))
                .Select(docId => documentTypeMap[docId])
                .Distinct()
                .ToList();

            return Task.FromResult(typeIds.AsEnumerable());
        }

        /// <inheritdoc />
        public Task<string> GetSelectedTypeIdsTextAsync(string sessionId, IDictionary<string, string> documentTypeMap)
        {
            var typeIds = GetSelectedTypeIdsAsync(sessionId, documentTypeMap).Result; // .Result porque es async pero podemos hacer await. Para simplificar usamos .Result en este método síncrono.
            var text = string.Join(Environment.NewLine, typeIds);
            return Task.FromResult(text);
        }

        /// <inheritdoc />
        public Task ClearSelectionAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("SessionId no puede ser nulo o vacío");

            var key = GetCacheKey(sessionId);
            _cache.Remove(key);
            _logger.LogDebug("Selección eliminada para sesión {SessionId}", sessionId);
            return Task.CompletedTask;
        }
    }
}