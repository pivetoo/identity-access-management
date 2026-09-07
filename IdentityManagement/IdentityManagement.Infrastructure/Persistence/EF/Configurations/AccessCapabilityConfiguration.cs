using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class AccessCapabilityConfiguration : IEntityTypeConfiguration<AccessCapability>
    {
        public void Configure(EntityTypeBuilder<AccessCapability> builder)
        {
            builder.ToTable("accesscapabilities");

            builder.HasOne(entity => entity.SystemApplication)
                .WithMany()
                .HasForeignKey(entity => entity.SystemApplicationId);

            builder.Property(entity => entity.CapabilityKey)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Module)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(entity => entity.ModuleLabel)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Label)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasIndex(entity => new { entity.SystemApplicationId, entity.CapabilityKey })
                .IsUnique();
        }
    }
}
