using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OAuthClientScopeConfiguration : IEntityTypeConfiguration<OAuthClientScope>
    {
        public void Configure(EntityTypeBuilder<OAuthClientScope> builder)
        {
            builder.ToTable("oauthclientscopes");

            builder.HasOne(entity => entity.OAuthClient)
                .WithMany(entity => entity.Scopes)
                .HasForeignKey(entity => entity.OAuthClientId);

            builder.HasOne(entity => entity.OAuthScope)
                .WithMany(entity => entity.Clients)
                .HasForeignKey(entity => entity.OAuthScopeId);

            builder.HasIndex(entity => new { entity.OAuthClientId, entity.OAuthScopeId })
                .IsUnique();
        }
    }
}
