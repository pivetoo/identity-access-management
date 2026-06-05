using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SystemIntegrationConfiguration : IEntityTypeConfiguration<SystemIntegration>
    {
        public void Configure(EntityTypeBuilder<SystemIntegration> builder)
        {
            builder.ToTable("systemintegrations");

            builder.Property(entity => entity.SystemApplicationId)
                .HasColumnName("systemapplicationid");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(entity => entity.BaseUrl)
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnName("baseurl");

            builder.Property(entity => entity.IsActive)
                .HasColumnName("isactive");

            builder.HasMany(entity => entity.Parameters)
                .WithOne()
                .HasForeignKey(entity => entity.SystemIntegrationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => entity.SystemApplicationId);
        }
    }
}
