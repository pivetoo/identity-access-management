using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class AuthorizationCodeConfiguration : IEntityTypeConfiguration<AuthorizationCode>
    {
        public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
        {
            builder.ToTable("authorizationcodes");

            builder.Property(entity => entity.Code)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(entity => entity.Scopes)
                .HasMaxLength(2000);

            builder.Property(entity => entity.RedirectUri)
                .HasMaxLength(2000);

            builder.Property(entity => entity.SessionId)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasOne(entity => entity.User)
                .WithMany(entity => entity.AuthorizationCodes)
                .HasForeignKey(entity => entity.UserId);

            builder.HasOne(entity => entity.Contract)
                .WithMany(entity => entity.AuthorizationCodes)
                .HasForeignKey(entity => entity.ContractId);

            builder.HasIndex(entity => entity.Code)
                .IsUnique();
        }
    }
}
