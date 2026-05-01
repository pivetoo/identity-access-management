using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SigningKeyConfiguration : IEntityTypeConfiguration<SigningKey>
    {
        public void Configure(EntityTypeBuilder<SigningKey> builder)
        {
            builder.ToTable("signingkeys");

            builder.Property(entity => entity.KeyId)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Algorithm)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(entity => entity.PublicKeyPem)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(entity => entity.PrivateKeyEncrypted)
                .HasMaxLength(8000);

            builder.HasIndex(entity => entity.KeyId)
                .IsUnique();
        }
    }
}
