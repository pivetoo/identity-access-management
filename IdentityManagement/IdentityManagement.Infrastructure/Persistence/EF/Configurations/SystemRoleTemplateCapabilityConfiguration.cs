using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SystemRoleTemplateCapabilityConfiguration : IEntityTypeConfiguration<SystemRoleTemplateCapability>
    {
        public void Configure(EntityTypeBuilder<SystemRoleTemplateCapability> builder)
        {
            builder.ToTable("systemroletemplatecapabilities");

            builder.HasOne(entity => entity.SystemRoleTemplate)
                .WithMany()
                .HasForeignKey(entity => entity.SystemRoleTemplateId);

            builder.Property(entity => entity.CapabilityKey)
                .IsRequired()
                .HasMaxLength(120);

            builder.HasIndex(entity => new { entity.SystemRoleTemplateId, entity.CapabilityKey })
                .IsUnique();
        }
    }
}
