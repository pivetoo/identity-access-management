using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SystemRoleTemplateAccessResourceConfiguration : IEntityTypeConfiguration<SystemRoleTemplateAccessResource>
    {
        public void Configure(EntityTypeBuilder<SystemRoleTemplateAccessResource> builder)
        {
            builder.ToTable("systemroletemplateaccessresources");

            builder.HasOne(entity => entity.SystemRoleTemplate)
                .WithMany(entity => entity.SystemRoleTemplateAccessResources)
                .HasForeignKey(entity => entity.SystemRoleTemplateId);

            builder.HasOne(entity => entity.AccessResource)
                .WithMany()
                .HasForeignKey(entity => entity.AccessResourceId);

            builder.HasIndex(entity => new { entity.SystemRoleTemplateId, entity.AccessResourceId })
                .IsUnique();
        }
    }
}
