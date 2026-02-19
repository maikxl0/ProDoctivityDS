using AutoMapper;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class LogService : ILogService
    {
        private readonly IActivityLogRepository _logRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<LogService> _logger;

        public LogService(
            IActivityLogRepository logRepository,
            IMapper mapper,
            ILogger<LogService> logger)
        {
            _logRepository = logRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task LogAsync(string level, string category, string message, string? documentId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var logEntry = new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Category = category,
                    Message = message,
                    DocumentId = documentId
                };

                await _logRepository.AddAsync(logEntry, cancellationToken);
                _logger.LogDebug("Log registrado: {Level} - {Category} - {Message}", level, category, message);
            }
            catch (Exception ex)
            {
                // Si falla el guardado del log, al menos lo registramos en el logger de aplicación
                _logger.LogError(ex, "Error al guardar log en base de datos: {Level} - {Category} - {Message}", level, category, message);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ActivityLogEntryDto>> GetRecentLogsAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            var logs = await _logRepository.GetRecentAsync(limit, cancellationToken);
            return _mapper.Map<IEnumerable<ActivityLogEntryDto>>(logs);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ActivityLogEntryDto>> GetLogsByLevelAsync(string level, int limit = 100, CancellationToken cancellationToken = default)
        {
            // Podríamos agregar este método al repositorio si es necesario,
            // pero por ahora filtramos en memoria (los logs no suelen ser masivos)
            var logs = await _logRepository.GetRecentAsync(limit * 2, cancellationToken); // pedimos un poco más para asegurar
            var filtered = logs.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).Take(limit);
            return _mapper.Map<IEnumerable<ActivityLogEntryDto>>(filtered);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ActivityLogEntryDto>> GetLogsByDocumentIdAsync(string documentId, int limit = 100, CancellationToken cancellationToken = default)
        {
            var logs = await _logRepository.GetByDocumentIdAsync(documentId, cancellationToken);
            var limited = logs.OrderByDescending(l => l.Timestamp).Take(limit);
            return _mapper.Map<IEnumerable<ActivityLogEntryDto>>(limited);
        }

        /// <inheritdoc />
        public async Task ClearLogsAsync(CancellationToken cancellationToken = default)
        {
            await _logRepository.ClearAsync(cancellationToken);
            _logger.LogInformation("Todos los logs han sido eliminados");
        }
    }
}