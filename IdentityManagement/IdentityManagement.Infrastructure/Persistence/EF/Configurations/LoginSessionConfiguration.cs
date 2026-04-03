using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class LoginSessionConfiguration : IEntityTypeConfiguration<LoginSession>
    {
        public void Configure(EntityTypeBuilder<LoginSession> builder)
        {
            builder.ToTable("LoginSessions");

            builder.Property(entity => entity.SessionId)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(entity => entity.IpAddress)
                .HasMaxLength(45);

            builder.Property(entity => entity.UserAgent)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.User)
                .WithMany()
                .HasForeignKey(entity => entity.UserId);

            builder.HasOne(entity => entity.Contract)
                .WithMany()
                .HasForeignKey(entity => entity.ContractId);

            builder.HasIndex(entity => entity.SessionId)
                .IsUnique();
        }
    }
}
