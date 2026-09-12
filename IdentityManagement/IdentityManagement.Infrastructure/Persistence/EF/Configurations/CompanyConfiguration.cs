using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("companies");

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

            builder.Property(entity => entity.TenantId)
                .IsRequired();

            builder.Property(entity => entity.BillingPostalCode).HasMaxLength(8);
            builder.Property(entity => entity.BillingStreet).HasMaxLength(255);
            builder.Property(entity => entity.BillingNumber).HasMaxLength(20);
            builder.Property(entity => entity.BillingComplement).HasMaxLength(100);
            builder.Property(entity => entity.BillingDistrict).HasMaxLength(100);
            builder.Property(entity => entity.BillingCity).HasMaxLength(100);
            builder.Property(entity => entity.BillingState).HasMaxLength(2);
            builder.Property(entity => entity.UtmSource).HasMaxLength(200);
            builder.Property(entity => entity.UtmMedium).HasMaxLength(200);
            builder.Property(entity => entity.UtmCampaign).HasMaxLength(200);
            builder.Property(entity => entity.UtmContent).HasMaxLength(200);
            builder.Property(entity => entity.UtmTerm).HasMaxLength(200);
            builder.Property(entity => entity.Gclid).HasMaxLength(200);
            builder.Property(entity => entity.Fbclid).HasMaxLength(200);
            builder.Property(entity => entity.LandingPage).HasMaxLength(500);
            builder.Property(entity => entity.Referrer).HasMaxLength(500);

            builder.HasIndex(entity => entity.TenantId)
                .IsUnique();

            builder.HasIndex(entity => entity.Document)
                .IsUnique();
        }
    }
}
