using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");

            builder.Property(entity => entity.LegalName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.TradeName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Document)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(entity => entity.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(entity => entity.PhoneNumber)
                .HasMaxLength(30);

            builder.HasIndex(entity => entity.Document)
                .IsUnique();

            builder.HasIndex(entity => entity.Email)
                .IsUnique();
        }
    }
}
