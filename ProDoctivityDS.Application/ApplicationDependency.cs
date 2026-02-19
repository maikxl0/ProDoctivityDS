using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Application.Mappings;
using ProDoctivityDS.Application.Services;


namespace ProDoctivityDS.Application
{
    public static class ApplicationDependency
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {

            services.AddScoped(typeof(IBaseService<,>), typeof(BaseServices<,>));

            services.AddMemoryCache();
            services.AddHttpClient();

            // Servicios de aplicación
            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<ISelectionService, SelectionService>();
            services.AddScoped<IProcessingService, ProcessingService>();
            services.AddScoped<IProcessingProgressStore, ProcessingProgressStore>();
            services.AddScoped<IAnalysisService, AnalysisService>();
            services.AddScoped<ILogService, LogService>();

            services.AddScoped<IProductivityApiClient, ProductivityApiClient>();
            services.AddScoped<IFileStorageService, FileStorageService>();

            // AutoMapper
            services.AddAutoMapper(typeof(MappingProfile));
        }
    }
}
