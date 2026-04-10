using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using System.Text.Json;

namespace ProDoctivityDS.Persistence.Repositories
{
    public class StoredConfigurationRepository : IStoredConfigurationRepository
    {
        private readonly IMapper _mapper;
        private readonly ILogger<StoredConfigurationRepository> _logger;
        private readonly string _filePath;
        private readonly object _lock = new object();
        private StoredConfiguration? _configuration;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public StoredConfigurationRepository(
            IMapper mapper,
            IConfiguration configuration,
            ILogger<StoredConfigurationRepository> logger)
        {
            _mapper = mapper;
            _logger = logger;
            var contentRootPath = Directory.GetCurrentDirectory();

            var appDataPath = configuration["AppData:BasePath"];
            if (string.IsNullOrWhiteSpace(appDataPath))
            {
                appDataPath = Environment.GetEnvironmentVariable("APP_DATA_DIR");
            }

            if (string.IsNullOrWhiteSpace(appDataPath))
            {
                appDataPath = Path.Combine(contentRootPath, "App_Data");
            }
            else if (!Path.IsPathRooted(appDataPath))
            {
                appDataPath = Path.GetFullPath(Path.Combine(contentRootPath, appDataPath));
            }

            var configuredFilePath = configuration["Persistence:ConfigurationFilePath"];
            if (string.IsNullOrWhiteSpace(configuredFilePath))
            {
                _filePath = Path.Combine(appDataPath, "stored-configuration.json");
            }
            else if (Path.IsPathRooted(configuredFilePath))
            {
                _filePath = configuredFilePath;
            }
            else
            {
                _filePath = Path.GetFullPath(Path.Combine(contentRootPath, configuredFilePath));
            }

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public Task<StoredConfiguration> GetActiveConfigurationAsync()
        {
            lock (_lock)
            {
                EnsureLoaded();

                if (_configuration == null)
                {
                    return Task.FromResult(new StoredConfiguration());
                }

                var copy = _mapper.Map<StoredConfiguration>(_configuration);
                return Task.FromResult(copy);
            }
        }

        public Task UpdateConfigurationAsync(StoredConfiguration configuration)
        {
            lock (_lock)
            {
                EnsureLoaded();

                var entityToStore = _mapper.Map<StoredConfiguration>(configuration);
                entityToStore.ProcessingOptionsJson = configuration.ProcessingOptionsJson;
                entityToStore.AnalysisRulesJson = configuration.AnalysisRulesJson;
                entityToStore.LastModified = DateTime.UtcNow;

                _configuration = entityToStore;
                PersistConfiguration();
            }

            return Task.CompletedTask;
        }

        private void EnsureLoaded()
        {
            if (_configuration != null)
            {
                return;
            }

            if (!File.Exists(_filePath))
            {
                _configuration = new StoredConfiguration();
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                _configuration = JsonSerializer.Deserialize<StoredConfiguration>(json, JsonOptions) ?? new StoredConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo cargar la configuracion persistida desde {FilePath}. Se usara configuracion vacia.",
                    _filePath);
                _configuration = new StoredConfiguration();
            }
        }

        private void PersistConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(_configuration, JsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo guardar la configuracion persistida en {FilePath}", _filePath);
                throw;
            }
        }
    }
}
