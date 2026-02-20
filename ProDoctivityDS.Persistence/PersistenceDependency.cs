using Domain.Interfaces.Repositories;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Domain.Interfaces;
using ProDoctivityDS.Persistence.Context;
using ProDoctivityDS.Persistence.Repositories;

namespace ProDoctivityDS.Persistence
{
    public static class PersistenceDependency
    {
        public static void AddPersistenceDependencies(this IServiceCollection services, IConfiguration config)
        {
            #region Contexts
            // DbContext
            services.AddDbContext<ProDoctivityDSDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("ConnectionDb")).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            // Repositorios
            services.AddScoped<IStoredConfigurationRepository, StoredConfigurationRepository>();
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
            services.AddScoped<IProcessedDocumentRepository, ProcessedDocumentRepository>();

            // Cliente HTTP para API de Productivity
            //services.AddHttpClient<IProductivityApiClient, ProductivityApiClient>();

            // Servicios de infraestructura
            //services.AddScoped<IFileStorageService, FileStorageService>();
            

            // Configuración de FileStorage (opcional)
            //services.Configure<FileStorageSettings>(
            //    config.GetSection("FileStorage"));

            // Store de progreso (singleton)
            //services.AddSingleton<ProcessingProgressStore>()

            #endregion
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        }
    }
}
