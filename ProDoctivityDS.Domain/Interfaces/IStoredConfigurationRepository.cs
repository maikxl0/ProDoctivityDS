using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Domain.Interfaces
{
    public interface IStoredConfigurationRepository 
    {
        /// <summary>
        /// Obtiene la configuración del usuario actual (resuelve automáticamente vía ICurrentUserService).
        /// Si no hay usuario, retorna la configuración por defecto.
        /// </summary>
        Task<StoredConfiguration> GetActiveConfigurationAsync();

        /// <summary>
        /// Obtiene la configuración por defecto/plantilla (credenciales API compartidas).
        /// </summary>
        Task<StoredConfiguration> GetDefaultConfigurationAsync();

        /// <summary>
        /// Obtiene la configuración para un usuario específico.
        /// Si no existe, crea una nueva basada en la plantilla por defecto.
        /// </summary>
        Task<StoredConfiguration> GetConfigurationForUserAsync(string username);

        /// <summary>
        /// Actualiza la configuración del usuario actual (resuelve automáticamente).
        /// </summary>
        Task UpdateConfigurationAsync(StoredConfiguration configuration);

        /// <summary>
        /// Actualiza la configuración para un usuario específico.
        /// </summary>
        Task UpdateConfigurationForUserAsync(string username, StoredConfiguration configuration);
    }
}

