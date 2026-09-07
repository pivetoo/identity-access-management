using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class RoleCapabilityConfiguration : IEntityTypeConfiguration<RoleCapability>
    {
        public void Configure(EntityTypeBuilder<RoleCapability> builder)
        {
            builder.ToTable("rolecapabilities");

            builder.HasOne(entity => entity.Role)
                .WithMany()
                .HasForeignKey(entity => entity.RoleId);

            builder.Property(entity => entity.CapabilityKey)
                .IsRequired()
                .HasMaxLength(120);

            builder.HasIndex(entity => new { entity.RoleId, entity.CapabilityKey })
                .IsUnique();
        }
    }
}
