using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refreshtokens");

            builder.Property(entity => entity.Token)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(entity => entity.SessionId)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(entity => entity.Scopes)
                .HasMaxLength(2000);

            builder.HasOne(entity => entity.User)
                .WithMany(entity => entity.RefreshTokens)
                .HasForeignKey(entity => entity.UserId);

            builder.HasOne(entity => entity.Contract)
                .WithMany(entity => entity.RefreshTokens)
                .HasForeignKey(entity => entity.ContractId);

            builder.HasIndex(entity => entity.Token)
                .IsUnique();
        }
    }
}
