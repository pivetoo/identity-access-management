using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("passwordresettokens");

            builder.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);

            builder.HasIndex(e => e.Token)
                .IsUnique();
        }
    }
}
