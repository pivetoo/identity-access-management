using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.Property(entity => entity.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(entity => entity.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.AvatarUrl)
                .HasMaxLength(500);

            builder.HasIndex(entity => entity.Username)
                .IsUnique();

            builder.HasIndex(entity => entity.Email)
                .IsUnique();
        }
    }
}
