using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using System.Collections.Concurrent;

namespace ProDoctivityDS.Application.Services
{
    /// <summary>
    /// Almacena el progreso de procesamiento por sesión.
    /// Registrado como Singleton.
    /// </summary>
    public class ProcessingProgressStore : IProcessingProgressStore
    {
        private readonly ConcurrentDictionary<string, (ProcessProgressDto Progress, DateTime LastAccessed)> _progress = new();
        private readonly ILogger<ProcessingProgressStore> _logger;
        public ProcessingProgressStore(ILogger<ProcessingProgressStore> logger)
        {
            _logger = logger;
        }

        public void UpdateProgress(string sessionId, ProcessProgressDto progress)
        {
            _progress[sessionId] = (progress, DateTime.UtcNow);
            _logger.LogInformation("Progreso actualizado para sesión {SessionId}: {Processed}/{Total} - {Status}",
                sessionId, progress.Processed, progress.Total, progress.Status);

            CleanupExpiredEntries();
        }

        public ProcessProgressDto? GetProgress(string sessionId)
        {
            var exists = _progress.TryGetValue(sessionId, out var entry);
            _logger.LogInformation("Consulta de progreso para sesión {SessionId}: {Encontrado}", sessionId, exists);
            return exists ? entry.Progress : null;
        }

        public void RemoveProgress(string sessionId)
        {
            _progress.TryRemove(sessionId, out _);
            _logger.LogInformation("Progreso eliminado para sesión {SessionId}", sessionId);
        }

        private void CleanupExpiredEntries()
        {
            var expiredKeys = _progress
                .Where(p => DateTime.UtcNow - p.Value.LastAccessed > TimeSpan.FromHours(1))
                .Select(p => p.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _progress.TryRemove(key, out _);
                _logger.LogInformation("Progreso expirado eliminado para sesión {SessionId}", key);
            }
        }
    }
}