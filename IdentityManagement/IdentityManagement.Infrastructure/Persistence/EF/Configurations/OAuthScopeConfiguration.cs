using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OAuthScopeConfiguration : IEntityTypeConfiguration<OAuthScope>
    {
        public void Configure(EntityTypeBuilder<OAuthScope> builder)
        {
            builder.ToTable("oauthscopes");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.DisplayName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasIndex(entity => entity.Name)
                .IsUnique();
        }
    }
}
