using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.PDFManipulator.Services;

namespace ProDoctivityDS.PDFManipulator
{
    public static class PDFDependency
    {
        public static IServiceCollection AddPdfServices(this IServiceCollection services)
        {
            // Servicios de PDF
            services.AddScoped<IPdfAnalyzer, PdfAnalyzerService>();
            services.AddScoped<IPdfManipulator, PdfManipulatorService>();

            return services;
        }
    }
}
