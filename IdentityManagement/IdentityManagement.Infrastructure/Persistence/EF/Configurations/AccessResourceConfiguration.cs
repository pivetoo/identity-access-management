using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class AccessResourceConfiguration : IEntityTypeConfiguration<AccessResource>
    {
        public void Configure(EntityTypeBuilder<AccessResource> builder)
        {
            builder.ToTable("accessresources");

            builder.HasOne(entity => entity.SystemApplication)
                .WithMany()
                .HasForeignKey(entity => entity.SystemApplicationId);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Area)
                .HasMaxLength(255);

            builder.Property(entity => entity.Controller)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Action)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.HttpMethod)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(entity => entity.Route)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(entity => entity.Capabilities)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(entity => new { entity.SystemApplicationId, entity.Name })
                .IsUnique();
        }
    }
}
