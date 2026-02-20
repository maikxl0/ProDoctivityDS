using AutoMapper;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ===== Entidades de dominio <-> DTOs de respuesta =====

            // Configuración
            CreateMap<StoredConfiguration, ConfigurationDto>()
    .ForMember(dest => dest.BearerToken, opt => opt.MapFrom(src => src.BearerToken));

            // Documentos
            //CreateMap<ProductivityDocument, DocumentDto>()
            //    .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            //    .ForMember(dest => dest.AnalysisStatus, opt => opt.MapFrom(src => src.AnalysisStatus.ToString()));

            // Logs
            CreateMap<ActivityLogEntry, ActivityLogEntryDto>()
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp.ToString("HH:mm:ss")));

            //// Documentos procesados (para reportes)
            //CreateMap<ProcessedDocument, ProcessedDocumentDto>()
            //    .ForMember(dest => dest.ProcessingDate, opt => opt.MapFrom(src => src.ProcessingDate.ToString("yyyy-MM-dd HH:mm:ss")));

            // ===== DTOs de solicitud <-> Entidades de dominio =====

            // Guardar configuración
            CreateMap<SaveConfigurationRequestDto, StoredConfiguration>()
                .ForMember(dest => dest.ApiBaseUrl, opt => opt.MapFrom(src => src.ApiCredentials.BaseUrl))
                .ForMember(dest => dest.ApiKey, opt => opt.MapFrom(src => src.ApiCredentials.ApiKey))
                .ForMember(dest => dest.ApiSecret, opt => opt.MapFrom(src => src.ApiCredentials.ApiSecret))
                .ForMember(dest => dest.BearerToken, opt => opt.MapFrom(src => src.ApiCredentials.BearerToken))
                .ForMember(dest => dest.CookieSessionId, opt => opt.MapFrom(src => src.ApiCredentials.CookieSessionId))
                .ForMember(dest => dest.ProcessingOptions, opt => opt.MapFrom(src => src.ProcessingOptions))
                .ForMember(dest => dest.AnalysisRules, opt => opt.MapFrom(src => src.AnalysisRules));

            // ===== Value Objects y sus DTOs =====

            // Opciones de procesamiento
            CreateMap<ProcessingOptions, ProcessingOptionsDto>().ReverseMap();

            // Normalización
            CreateMap<NormalizationOptions, NormalizationOptionsDto>().ReverseMap();

            // Criterios
            CreateMap<Criterion, CriterionDto>().ReverseMap();

            // Conjunto de reglas de análisis
            CreateMap<AnalysisRuleSet, AnalysisRuleSetDto>().ReverseMap();

            // Credenciales API (para prueba de conexión)
            CreateMap<ApiCredentialsDto, StoredConfiguration>()
                .ForMember(dest => dest.ApiBaseUrl, opt => opt.MapFrom(src => src.BaseUrl))
                .ForMember(dest => dest.ApiKey, opt => opt.MapFrom(src => src.ApiKey))
                .ForMember(dest => dest.ApiSecret, opt => opt.MapFrom(src => src.ApiSecret))
                .ForMember(dest => dest.BearerToken, opt => opt.MapFrom(src => src.BearerToken))
                .ForMember(dest => dest.CookieSessionId, opt => opt.MapFrom(src => src.CookieSessionId));

            // De DTO de infraestructura a entidad de dominio
            CreateMap<ProductivityDocumentDto, ProductivityDocument>()
                .ForMember(dest => dest.DocumentId, opt => opt.MapFrom(src => src.DocumentId ?? src.Id))
                .ForMember(dest => dest.LastDocumentVersionId, opt => opt.MapFrom(src => src.LastDocumentVersionId ?? src.DocumentVersionId))
                .ForMember(dest => dest.AnalysisStatus, opt => opt.Ignore()); // Se asigna después

            // De entidad de dominio a DTO de respuesta
            CreateMap<ProductivityDocument, DocumentDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.AnalysisStatus, opt => opt.MapFrom(src => src.AnalysisStatus.ToString()));

            // Si usas el DTO de infraestructura directamente en algún lado (no recomendado), pero por si acaso:
            CreateMap<ProductivityDocumentDto, DocumentDto>()
                .ForMember(dest => dest.DocumentId, opt => opt.MapFrom(src => src.DocumentId ?? src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.DocumentTypeName, opt => opt.MapFrom(src => src.DocumentTypeName))
                .ForMember(dest => dest.DocumentTypeId, opt => opt.MapFrom(src => src.DocumentTypeId))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt ?? 0))
                .ForMember(dest => dest.AnalysisStatus, opt => opt.Ignore());
        }
    }
}