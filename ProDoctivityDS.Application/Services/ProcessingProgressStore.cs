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
        private readonly ConcurrentDictionary<string, ProcessProgressDto> _progress = new();

        public void UpdateProgress(string sessionId, ProcessProgressDto progress)
        {
            _progress[sessionId] = progress;
        }

        public ProcessProgressDto? GetProgress(string sessionId)
        {
            return _progress.TryGetValue(sessionId, out var progress) ? progress : null;
        }

        public void RemoveProgress(string sessionId)
        {
            _progress.TryRemove(sessionId, out _);
        }
    }
}