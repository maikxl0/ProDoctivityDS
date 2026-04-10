using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProDoctivityDS.Persistence.Repositories
{
    public class StoredConfigurationRepository : IStoredConfigurationRepository
    {
        private readonly ILogger<StoredConfigurationRepository> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly string _basePath;
        private readonly string _defaultFilePath;
        private readonly object _defaultLock = new();
        private readonly ConcurrentDictionary<string, (StoredConfiguration Config, object Lock)> _userConfigs = new();
        private StoredConfiguration? _defaultConfiguration;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public StoredConfigurationRepository(
            IConfiguration configuration,
            ILogger<StoredConfigurationRepository> logger,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _currentUserService = currentUserService;

            var contentRootPath = Directory.GetCurrentDirectory();
            var appDataPath = configuration["AppData:BasePath"]
                ?? Environment.GetEnvironmentVariable("APP_DATA_DIR");

            if (string.IsNullOrWhiteSpace(appDataPath))
                appDataPath = Path.Combine(contentRootPath, "App_Data");
            else if (!Path.IsPathRooted(appDataPath))
                appDataPath = Path.GetFullPath(Path.Combine(contentRootPath, appDataPath));

            _basePath = appDataPath;

            var configuredFilePath = configuration["Persistence:ConfigurationFilePath"];
            if (string.IsNullOrWhiteSpace(configuredFilePath))
                _defaultFilePath = Path.Combine(appDataPath, "stored-configuration.json");
            else if (Path.IsPathRooted(configuredFilePath))
                _defaultFilePath = configuredFilePath;
            else
                _defaultFilePath = Path.GetFullPath(Path.Combine(contentRootPath, configuredFilePath));

            Directory.CreateDirectory(_basePath);
        }

        public Task<StoredConfiguration> GetActiveConfigurationAsync()
        {
            var username = _currentUserService.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
                return GetDefaultConfigurationAsync();

            return GetConfigurationForUserAsync(username);
        }

        public Task<StoredConfiguration> GetDefaultConfigurationAsync()
        {
            lock (_defaultLock)
            {
                if (_defaultConfiguration == null)
                    _defaultConfiguration = LoadFromFile(_defaultFilePath);

                return Task.FromResult(Clone(_defaultConfiguration));
            }
        }

        public Task<StoredConfiguration> GetConfigurationForUserAsync(string username)
        {
            var safeUsername = SanitizeUsername(username);
            var entry = _userConfigs.GetOrAdd(safeUsername, _ => (null!, new object()));

            lock (entry.Lock)
            {
                if (entry.Config != null)
                    return Task.FromResult(Clone(entry.Config));

                var userPath = GetUserConfigPath(safeUsername);
                StoredConfiguration config;

                if (File.Exists(userPath))
                {
                    config = LoadFromFile(userPath);
                    _logger.LogInformation("Configuración cargada para usuario {Username}", username);
                }
                else
                {
                    // Nuevo usuario: heredar de la plantilla por defecto
                    lock (_defaultLock)
                    {
                        if (_defaultConfiguration == null)
                            _defaultConfiguration = LoadFromFile(_defaultFilePath);
                    }
                    config = Clone(_defaultConfiguration!);
                    config.Username = username;
                    config.Password = null;
                    config.BearerToken = string.Empty;
                    SaveToFile(userPath, config);
                    _logger.LogInformation("Configuración creada para nuevo usuario {Username}", username);
                }

                _userConfigs[safeUsername] = (config, entry.Lock);
                return Task.FromResult(Clone(config));
            }
        }

        public Task UpdateConfigurationAsync(StoredConfiguration configuration)
        {
            var username = _currentUserService.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                // Actualizar plantilla por defecto
                lock (_defaultLock)
                {
                    configuration.LastModified = DateTime.UtcNow;
                    _defaultConfiguration = Clone(configuration);
                    SaveToFile(_defaultFilePath, configuration);
                }
                return Task.CompletedTask;
            }

            return UpdateConfigurationForUserAsync(username, configuration);
        }

        public Task UpdateConfigurationForUserAsync(string username, StoredConfiguration configuration)
        {
            var safeUsername = SanitizeUsername(username);
            var entry = _userConfigs.GetOrAdd(safeUsername, _ => (null!, new object()));

            lock (entry.Lock)
            {
                configuration.LastModified = DateTime.UtcNow;
                var cloned = Clone(configuration);
                _userConfigs[safeUsername] = (cloned, entry.Lock);

                var userPath = GetUserConfigPath(safeUsername);
                SaveToFile(userPath, configuration);
                _logger.LogInformation("Configuración actualizada para usuario {Username}", username);
            }

            return Task.CompletedTask;
        }

        private string GetUserConfigPath(string safeUsername)
        {
            var userDir = Path.Combine(_basePath, "users", safeUsername);
            Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "config.json");
        }

        private static string SanitizeUsername(string username)
        {
            // Reemplazar caracteres no válidos para nombres de directorio
            return Regex.Replace(username.ToLowerInvariant().Trim(), @"[^a-z0-9._@-]", "_");
        }

        private StoredConfiguration Clone(StoredConfiguration source)
        {
            var json = JsonSerializer.Serialize(source, JsonOptions);
            return JsonSerializer.Deserialize<StoredConfiguration>(json, JsonOptions) ?? new StoredConfiguration();
        }

        private StoredConfiguration LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new StoredConfiguration();

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<StoredConfiguration>(json, JsonOptions) ?? new StoredConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo cargar configuración desde {FilePath}", filePath);
                return new StoredConfiguration();
            }
        }

        private void SaveToFile(string filePath, StoredConfiguration config)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo guardar configuración en {FilePath}", filePath);
                throw;
            }
        }
    }
}
