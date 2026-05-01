using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OAuthClientRedirectUriConfiguration : IEntityTypeConfiguration<OAuthClientRedirectUri>
    {
        public void Configure(EntityTypeBuilder<OAuthClientRedirectUri> builder)
        {
            builder.ToTable("oauthclientredirecturis");

            builder.Property(entity => entity.Uri)
                .IsRequired()
                .HasMaxLength(2000);

            builder.HasOne(entity => entity.OAuthClient)
                .WithMany(entity => entity.RedirectUris)
                .HasForeignKey(entity => entity.OAuthClientId);

            builder.HasIndex(entity => new { entity.OAuthClientId, entity.Uri, entity.Type })
                .IsUnique();
        }
    }
}
