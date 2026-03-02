using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Interfaces;
using ProDoctivityDS.Shared.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ProDoctivityDS.Shared
{
    public static class SharedDependency
    {
        public static void AddSharedServices(this IServiceCollection services)
        {
            services.AddDataProtection();
            services.AddSingleton<IEncryptionService, EncryptionService>();

            services.AddScoped<IPdfAnalyzer, PdfAnalyzerService>();
            services.AddScoped<IPdfManipulator, PdfManipulatorService>();
        }
            
        
    }
}
