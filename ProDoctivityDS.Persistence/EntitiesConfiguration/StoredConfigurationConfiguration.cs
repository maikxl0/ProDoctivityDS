using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Persistence.EntitiesConfiguration
{
    public class StoredConfigurationConfiguration : IEntityTypeConfiguration<StoredConfiguration>
    {
        public void Configure(EntityTypeBuilder<StoredConfiguration> builder)
        {
            builder.Property(c => c.Username)
           .HasMaxLength(100)               // Longitud máxima opcional
           .IsRequired(false);               // Puede ser nulo si no se usa login automático

            builder.Property(c => c.Password)
                   .HasMaxLength(255)                // Suficiente para contraseñas o hash
                   .IsRequired(false);

            // Garantizar que solo exista una fila (singleton)
            builder.HasIndex(c => c.Id)
                   .HasDatabaseName("IX_StoredConfigurations_Singleton")
                   .IsUnique();

            // Configurar columnas JSON (opcional, pero útil para restricciones)
            builder.Property(c => c.ProcessingOptionsJson)
                   .HasColumnType("TEXT")
                   .HasDefaultValue("{}");

            builder.Property(c => c.AnalysisRulesJson)
                   .HasColumnType("TEXT")
                   .HasDefaultValue("{}");

            // Índice por LastModified (útil para ordenar)
            builder.HasIndex(c => c.LastModified)
                   .HasDatabaseName("IX_StoredConfigurations_LastModified");
        }
    }
}
