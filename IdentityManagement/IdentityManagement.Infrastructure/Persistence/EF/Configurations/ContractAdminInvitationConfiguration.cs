using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ContractAdminInvitationConfiguration : IEntityTypeConfiguration<ContractAdminInvitation>
    {
        public void Configure(EntityTypeBuilder<ContractAdminInvitation> builder)
        {
            builder.ToTable("contractadmininvitations");

            builder.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasOne(e => e.Contract)
                .WithMany()
                .HasForeignKey(e => e.ContractId);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false);

            builder.HasIndex(e => e.Token)
                .IsUnique();
        }
    }
}
