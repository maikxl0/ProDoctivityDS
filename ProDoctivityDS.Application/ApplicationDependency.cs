using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Application.Mappings;
using ProDoctivityDS.Application.Services;

namespace ProDoctivityDS.Application
{
    public static class ApplicationDependency
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileStorageSettingsDto>(configuration.GetSection("FileStorage"));
            services.AddMemoryCache();
            services.AddHttpClient();

            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<ISelectionService, SelectionService>();
            services.AddScoped<IProcessingService, ProcessingService>();
            services.AddScoped<IAnalysisService, AnalysisService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
            services.AddScoped<IDocumentDeletionService, DocumentDeletionService>();
            services.AddScoped<IProductivityApiClient, ProductivityApiClient>();
            services.AddScoped<IFileStorageService, FileStorageService>();

            services.AddSingleton<IProcessingProgressStore, ProcessingProgressStore>();
            services.AddAutoMapper(config => { }, typeof(MappingProfile));
        }
    }
}
