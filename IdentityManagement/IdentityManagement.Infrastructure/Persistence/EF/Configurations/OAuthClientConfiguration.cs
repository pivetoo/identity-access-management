using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OAuthClientConfiguration : IEntityTypeConfiguration<OAuthClient>
    {
        public void Configure(EntityTypeBuilder<OAuthClient> builder)
        {
            builder.ToTable("oauthclients");

            builder.Property(entity => entity.ClientId)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.ClientName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.ClientSecretHash)
                .HasMaxLength(512);

            builder.HasOne(entity => entity.SystemApplication)
                .WithMany()
                .HasForeignKey(entity => entity.SystemApplicationId);

            builder.HasIndex(entity => entity.ClientId)
                .IsUnique();
        }
    }
}
