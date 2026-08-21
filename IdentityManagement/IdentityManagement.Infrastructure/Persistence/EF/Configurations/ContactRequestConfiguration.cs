using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
    {
        public void Configure(EntityTypeBuilder<ContactRequest> builder)
        {
            builder.ToTable("contactrequests");

            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(150);
            builder.Property(entity => entity.Email).IsRequired().HasMaxLength(255);
            builder.Property(entity => entity.PhoneNumber).HasMaxLength(30);
            builder.Property(entity => entity.CompanyName).HasMaxLength(150);
            builder.Property(entity => entity.Message).IsRequired().HasMaxLength(4000);
            builder.Property(entity => entity.SourceIp).HasMaxLength(64);

            builder.HasIndex(entity => entity.CreatedAt);
            builder.HasIndex(entity => entity.HandledAt);
        }
    }
}
