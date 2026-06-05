using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SystemIntegrationParameterConfiguration : IEntityTypeConfiguration<SystemIntegrationParameter>
    {
        public void Configure(EntityTypeBuilder<SystemIntegrationParameter> builder)
        {
            builder.ToTable("systemintegrationparameters");

            builder.Property(entity => entity.SystemIntegrationId)
                .HasColumnName("systemintegrationid");

            builder.Property(entity => entity.Key)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("key");

            builder.Property(entity => entity.Value)
                .HasColumnType("text")
                .HasColumnName("value");

            builder.Property(entity => entity.IsSecret)
                .HasColumnName("issecret");

            builder.Property(entity => entity.ValueSource)
                .HasConversion<int>()
                .HasColumnName("valuesource");

            builder.Property(entity => entity.SourceAudience)
                .HasMaxLength(200)
                .HasColumnName("sourceaudience");

            builder.HasIndex(entity => new { entity.SystemIntegrationId, entity.Key })
                .IsUnique();
        }
    }
}
