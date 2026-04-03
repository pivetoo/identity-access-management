using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class RoleAccessResourceConfiguration : IEntityTypeConfiguration<RoleAccessResource>
    {
        public void Configure(EntityTypeBuilder<RoleAccessResource> builder)
        {
            builder.ToTable("roleaccessresources");

            builder.HasOne(entity => entity.Role)
                .WithMany(entity => entity.RoleAccessResources)
                .HasForeignKey(entity => entity.RoleId);

            builder.HasOne(entity => entity.AccessResource)
                .WithMany(entity => entity.RoleAccessResources)
                .HasForeignKey(entity => entity.AccessResourceId);

            builder.HasIndex(entity => new { entity.RoleId, entity.AccessResourceId })
                .IsUnique();
        }
    }
}
