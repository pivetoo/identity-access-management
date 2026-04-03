using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class AccessResourceConfiguration : IEntityTypeConfiguration<AccessResource>
    {
        public void Configure(EntityTypeBuilder<AccessResource> builder)
        {
            builder.ToTable("AccessResources");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

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

            builder.HasIndex(entity => entity.Name)
                .IsUnique();
        }
    }
}
