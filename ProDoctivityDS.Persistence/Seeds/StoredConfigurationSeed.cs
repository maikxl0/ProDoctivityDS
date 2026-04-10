using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Persistence.Seeds
{
    public static class StoredConfigurationSeeder
    {
        public static async Task SeedDefaultConfigurationAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IStoredConfigurationRepository>();

            // Verificar si ya existe una configuración
            var existing = await repository.GetActiveConfigurationAsync();

            // Consideramos que no hay configuración si el Id es 0 y todos los campos relevantes están vacíos
            if (existing == null || string.IsNullOrEmpty(existing.ApiBaseUrl))
            {
                var defaultConfig = new StoredConfiguration
                {
                    ApiBaseUrl = "https://cloud.prodoctivity.com/api/",
                    ApiKey = "",
                    ApiSecret = "",
                    BearerToken = "",
                    CookieSessionId = "",
                    Username = "",
                    Password = "",
                    ProcessingOptions = new ProcessingOptions() {
                    RemoveFirstPage = true,
                    OnlyIfCriteriaMet = true,
                    UpdateApi = true,
                    SaveOriginalFiles = true,
                    AutoRemoveAllSeparators = false,
                    CreateBackup = true,
                    RemovePagesEnabled = true,
                    PagesToRemove = "1",
                    RemoveMode = "specific",
                    PageRangeStart = 0,
                    PageRangeEnd = 1,
                    AnalyzeAllPages = true,
                    ShowExtractedText = true
                    }, 
                    AnalysisRules = new AnalysisRuleSet()
                    {
                        KeywordSeparador = "SEPARADOR DE DOCUMENTOS",
                        KeywordCodigo = "DOC-001",
                        Normalization = new NormalizationOptions()
                        {
                            IsEnabled = true,
                            ToUpperCase = true,
                            RemoveAccents = true,
                            RemovePunctuation = true,
                            IgnoreLineBreaks = true,
                            TrimExtraSpaces = true,
                        },
                        SearchCharacterLimit = 25

                    }        
                };

                await repository.UpdateConfigurationAsync(defaultConfig);
            }
        }
    }
}