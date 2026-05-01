using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PendingAuthorizationSessionConfiguration : IEntityTypeConfiguration<PendingAuthorizationSession>
    {
        public void Configure(EntityTypeBuilder<PendingAuthorizationSession> builder)
        {
            builder.ToTable("pendingauthorizationsessions");

            builder.Property(entity => entity.Token)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(entity => entity.AuthorizeRequestHash)
                .HasMaxLength(128);

            builder.HasOne(entity => entity.User)
                .WithMany()
                .HasForeignKey(entity => entity.UserId);

            builder.HasIndex(entity => entity.Token)
                .IsUnique();
        }
    }
}
