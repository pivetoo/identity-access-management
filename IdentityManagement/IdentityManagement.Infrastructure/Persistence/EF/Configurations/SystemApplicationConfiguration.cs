using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SystemApplicationConfiguration : IEntityTypeConfiguration<SystemApplication>
    {
        public void Configure(EntityTypeBuilder<SystemApplication> builder)
        {
            builder.ToTable("systemapplications");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.RedirectUris)
                .HasMaxLength(2000);

            builder.Property(entity => entity.Audience)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(entity => entity.Name)
                .IsUnique();

            builder.HasIndex(entity => entity.Audience)
                .IsUnique();
        }
    }
}
