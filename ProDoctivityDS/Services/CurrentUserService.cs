using Microsoft.AspNetCore.Http;
using ProDoctivityDS.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ProDoctivityDS.Services
{
    /// <summary>
    /// Implementación que mapea Session IDs a usernames.
    /// Usa AsyncLocal para soporte de background tasks.
    /// Registrado como Singleton.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private static readonly ConcurrentDictionary<string, (string Username, DateTime LastAccessed)> _sessionUserMap = new();
        private static readonly AsyncLocal<string?> _overrideUsername = new();
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCurrentUsername()
        {
            // Prioridad 1: Override explícito (para background tasks)
            if (_overrideUsername.Value != null)
                return _overrideUsername.Value;

            // Prioridad 2: Desde header X-Session-Id del request HTTP
            var sessionId = _httpContextAccessor.HttpContext?.Request.Headers["X-Session-Id"].FirstOrDefault();
            if (sessionId != null && _sessionUserMap.TryGetValue(sessionId, out var entry))
            {
                // Actualizar last accessed
                _sessionUserMap[sessionId] = (entry.Username, DateTime.UtcNow);
                return entry.Username;
            }

            return null;
        }

        public void SetUsername(string sessionId, string username)
        {
            _sessionUserMap[sessionId] = (username, DateTime.UtcNow);
            CleanupExpiredSessions();
        }

        public void RemoveSession(string sessionId)
        {
            _sessionUserMap.TryRemove(sessionId, out _);
        }

        public IDisposable OverrideUsername(string? username)
        {
            _overrideUsername.Value = username;
            return new OverrideScope();
        }

        private void CleanupExpiredSessions()
        {
            var expired = _sessionUserMap
                .Where(p => DateTime.UtcNow - p.Value.LastAccessed > TimeSpan.FromHours(4))
                .Select(p => p.Key)
                .ToList();

            foreach (var key in expired)
                _sessionUserMap.TryRemove(key, out _);
        }

        private class OverrideScope : IDisposable
        {
            public void Dispose() => _overrideUsername.Value = null;
        }
    }
}
